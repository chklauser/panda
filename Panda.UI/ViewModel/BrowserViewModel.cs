using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using JetBrains.Annotations;
using Panda.Core.Blocks;
using Panda.ServiceModel;
using Panda.UI.Properties;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Text;

namespace Panda.UI.ViewModel
{
    public class BrowserViewModel : IDisposable, INotifyPropertyChanged
    {
        private readonly ObservableCollection<DiskViewModel> _openDisks = new ObservableCollection<DiskViewModel>();
        private string _statusText;
        private Uri _serverUrl;
        private IServiceClient _serviceClient;
        private readonly ObservableCollection<DiskRecord> _serverDiskRecords = new ObservableCollection<DiskRecord>();

        public BrowserViewModel()
        {
            _openDisks.CollectionChanged += _openDisksCollectionChanged;
            _serverUrl = new Uri(Settings.Default.ServerUrl);
        }

        void _openDisksCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_openDisks.Count == 0)
            {
                StatusText = "No disk opened. Use File > Open Disk… to open a new disk or New Disk… to create a new one.";
            }
        }

        public ObservableCollection<DiskViewModel> OpenDisks
        {
            get { return _openDisks; }
        }

        #region Infrastructure

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var virtualDisk in OpenDisks)
                    virtualDisk.Disk.Dispose();

                OpenDisks.Clear();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // This method makes use of a bit of compiler black magic
        // Any optional method argument annotated with [CallerMemberName]
        // will automatically be supplied with the name of the caller
        // e.g. if you call it from StatusText, the propertyName will be "StatusText"
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "For the CallerMemberName mechanism (compiler automatically supplied name of calling method, in this case usually the property being changed) to work, the parameter needs to be optional. See http://msdn.microsoft.com/en-us/library/system.runtime.compilerservices.callermembernameattribute.aspx"), 
            NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public string StatusText
        {
            get { return _statusText; }
            set
            {
                if (value == _statusText) return;
                _statusText = value;
                OnPropertyChanged();
            }
        }

        public Uri ServerUrl
        {
            get { return _serverUrl; }
            set
            {
                if (value == _serverUrl) return;
                _serverUrl = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<DiskRecord> ServerDiskRecords
        {
            get { return _serverDiskRecords; }
        }

        public bool IsConnected
        {
            get { return _serviceClient != null; }
        }

        public bool CanConnect
        {
            get { return !IsConnected; }
        }

        private void _resetServiceClient()
        {
            if (_serviceClient != null)
                _serviceClient.Dispose();

            _serviceClient = ServerUrl != null ? new JsonServiceClient(ServerUrl.ToString()) : null;
            OnPropertyChanged("IsConnected");
            OnPropertyChanged("CanConnect");
        }

        public async Task ConnectAsync()
        {
            try
            {
                _resetServiceClient();
                await RefreshServerDisksAsync();
            }
            catch (WebServiceException e)
            {
                MessageBox.Show(e.Message, "Server communication error", MessageBoxButton.OK, MessageBoxImage.Error);
                Disconnect();
            }
        }

        public Task RefreshServerDisksAsync()
        {
            return RefreshServerDisksAsync(false);
        }

        public async Task RefreshServerDisksAsync(bool suppressStatusMessage)
        {
            var resp = await _serviceClient.GetAsync(new GetDisks());
            ServerDiskRecords.Clear();
            foreach (var diskRecord in resp)
                ServerDiskRecords.Add(diskRecord);

            if(!suppressStatusMessage)
                StatusText = String.Format("Connected to server at {0}.", ServerUrl);
        }

        public bool CanAssociateDisk(DiskViewModel diskView)
        {
            return diskView != null && IsConnected && diskView.SynchronizingDisk.ServerAssociation == null;
        }

        public async Task AssociateDiskAsync(DiskViewModel diskView)
        {
            DiskRecord resp;
            try
            {
                resp = await _serviceClient.GetAsync(new GetDiskInfo { DiskName = diskView.Name });
            }
            catch (WebServiceException e)
            {
                if (e.StatusCode == 404)
                {
                    resp = null;
                }
                else
                {
                    throw;
                }
            }

            var capacity = diskView.Disk.Capacity;
            diskView.SynchronizingDisk.ServerAssociation = diskView.Name;

            if (resp == null)
            {
                // need to upload the disk first
                StatusText = String.Format("Disk {0} is being uploaded to the server. Please wait...", diskView.Name);
                try
                {
                    diskView.SynchronizingDisk.NotifySynchronized();
                    diskView.Disk.Dispose();
                    // ReSharper disable AssignNullToNotNullAttribute
                    diskView.Disk = null;
                    // ReSharper restore AssignNullToNotNullAttribute

                    using (var fs = new FileStream(diskView.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        var request = new UploadDisk
                            {
                                Capacity = capacity,
                                DiskName = diskView.Name,
                                RequestStream = fs
                            };
                        var requestUrl = String.Format("{0}{1}?Capacity={2}", _serverUrl,
                            request.ToUrl("PUT").Substring(1), capacity);

                        //resp = await _serviceClient.PutAsync(request);
                        var req = WebRequest.CreateHttp(requestUrl);
                        req.Method = "PUT";
                        req.Accept = "application/json";

                        var reqStr = await req.GetRequestStreamAsync();
                        await fs.CopyToAsync(reqStr);
                        var rawResp = await req.GetResponseAsync();
                        var responseStream = rawResp.GetResponseStream();
                        if (responseStream != null)
                        {
                            using (var respReader = new StreamReader(responseStream, Encoding.UTF8))
                            {
                                var s = new JsonSerializer<DiskRecord>();
                                resp = s.DeserializeFromReader(respReader);
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("Missing response from upload.");
                        }

                        StatusText = string.Format("Disk {0} successfully uploaded to server.", diskView.Name);
                        await RefreshServerDisksAsync(suppressStatusMessage: true);
                    }
                }
                finally
                {
                    diskView.Disk = VirtualDisk.OpenExisting(diskView.FileName);
                }
            }
            StatusText = String.Format("Disk {0} associated with server as {1}.", diskView.Name, resp.Name);
        }

        public async Task DownloadDisk([NotNull] DiskRecord diskRecord, Dispatcher dispatcher)
        {
            var str = await _serviceClient.GetAsync(new DownloadDisk { DiskName = diskRecord.Name });
            var fileName = diskRecord.Name + ".panda";
            StatusText = string.Format("Downloading disk {0}. Please wait...", diskRecord.Name);
            using (var fs = new FileStream(fileName, FileMode.CreateNew, FileAccess.Write))
            {
                await str.CopyToAsync(fs);
            }
            OpenDisk(fileName, dispatcher);
            StatusText = string.Format("Disk {0} downloaded successfully.", diskRecord.Name);
        }

        public bool CanDownloadDiks([NotNull] DiskRecord diskRecord)
        {
            return IsConnected && OpenDisks.All(diskViewModel => !diskRecord.Name.Equals(diskViewModel.Name)) && ! File.Exists(diskRecord.Name + ".panda");
        }

        public void OpenDisk(string fileName, Dispatcher dispatcher)
        {
            VirtualDisk vdisk = null;
            try
            {
                vdisk = VirtualDisk.OpenExisting(fileName);
                RegisterDisk(fileName, vdisk, dispatcher);
            }
            catch
            {
                // We weren't able to get the disk into the caring
                // hands of the view model. Close the file to prevent
                // more damage.
                if (vdisk != null)
                    vdisk.Dispose();
                throw;
            }
        }

        public void RegisterDisk(string fileName, VirtualDisk vdisk, [NotNull] Dispatcher dispatcher)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");
            if (vdisk == null)
                throw new ArgumentNullException("vdisk");
            if (dispatcher == null)
                throw new ArgumentNullException("dispatcher");
            
            var name = Path.GetFileNameWithoutExtension(fileName) ??
                "disk" + Interlocked.Increment(ref _uniqueDiskNameCounter);
            
            // have the virtual disk dispatch its notifications on the UI thread.
            Debug.Assert(dispatcher != null);
            vdisk.NotificationDispatcher = new WindowsDispatcherAdapter(dispatcher);

            var diskModel = new DiskViewModel
                {
                    Disk = vdisk,
                    Name = name,
                    FileName = fileName
                };
            OpenDisks.Add(diskModel);
        }

        public async void Synchronize(DiskViewModel diskModel)
        {
            StatusText = string.Format("Querying server about changes to disk {0}.", diskModel.Name);

            // Check when local disk and server were changed
            var localLastSynchronized = diskModel.Disk.LastTimeSynchronized;
            var changedBlockResponseTask =
                    _serviceClient.GetAsync(new ChangedBlocks
                        {
                            DiskName = diskModel.Name,
                            ChangesSince = localLastSynchronized
                        });
            var localHasChanges = !diskModel.SynchronizingDisk.GetJournalEntriesSince(localLastSynchronized)
                                    .IsEmpty();
            var remoteChanges = (await changedBlockResponseTask).Changes;
            var remoteHasChanges = remoteChanges.Count > 0;

            // Perform synchronization
            if (!remoteHasChanges && localHasChanges)
            {
                await _pushChangesToServerAsync(diskModel);
                StatusText = string.Format("Local changes to {0} successfully uploaded to the server.", diskModel.Name);
                await RefreshServerDisksAsync(suppressStatusMessage:true);
                diskModel.NotifyDiskChangedExternally();
            }
            else if(remoteHasChanges)
            {
                var buffer = new byte[diskModel.SynchronizingDisk.BlockSize];
                if (localHasChanges)
                {
                    // conflict --> revert local changes first
                    await _revertLocalChangesAsync(diskModel, buffer);
                }

                // merge remote changes into local disk
                await _mergeRemoteBlocksAsync(diskModel, remoteChanges, buffer);
                diskModel.NotifyDiskChangedExternally();

                StatusText = string.Format("Local disk {0} successfully updated with remote changes.", diskModel.Name);
            }
            else
            {
                StatusText = string.Format("No changes to be synchronized for disk {0}. Everything is up to date.", diskModel.Name);
            }
        }

        private async Task _mergeRemoteBlocksAsync(DiskViewModel diskModel, IList<ChangeRecord> remoteChanges, byte[] buffer)
        {
            var blocksProcessed = 0;
            foreach (var remoteChange in remoteChanges)
            {
                StatusText = string.Format("Merging remote changes to disk {0} into local disk. {1}/{2} blocks merged",
                    diskModel.Name, blocksProcessed, remoteChanges.Count);

                await _mergeRemoteBlockAsync(diskModel, (BlockOffset) remoteChange.BlockOffset, buffer);

                blocksProcessed++;
            }
        }

        private async Task _revertLocalChangesAsync(DiskViewModel diskModel, byte[] buffer)
        {
            var localLastSynchronized = diskModel.Disk.LastTimeSynchronized;
            var localChanges = diskModel.SynchronizingDisk.GetJournalEntriesSince(localLastSynchronized).ToArray();
            var blocksReverted = 0;
            foreach (var localChange in localChanges)
            {
                StatusText =
                    string.Format("Detected conflict with disk {0} on server. Reverting local changes {1}/{2}",
                        diskModel.Name, blocksReverted, localChanges.Length);
                // Merge blocks from server according to local changes
                await _mergeRemoteBlockAsync(diskModel, localChange.BlockOffset, buffer);
                blocksReverted++;
            }
        }

        private async Task _pushChangesToServerAsync(DiskViewModel diskModel)
        {
            // Mark this synchronization point, it needs to be included in the data uploaded to the server
            var localLastSynchronized = diskModel.Disk.LastTimeSynchronized;
            diskModel.SynchronizingDisk.NotifySynchronized();
            var localChanges = JournalEntry.ToJournalSet(diskModel.SynchronizingDisk.GetJournalEntriesSince(localLastSynchronized));

            // push to the server
            var blocksProcessed = 0;
            var buffer = new byte[diskModel.SynchronizingDisk.BlockSize];
            foreach (var localChange in localChanges)
            {
                StatusText =
                    string.Format("Pushing local changes to disk {0} to the server. {1}/{2} blocks uploaded.",
                        diskModel.Name, blocksProcessed, localChanges.Count);
                // read block from local disk into remporary buffer and then send it to the server
                diskModel.SynchronizingDisk.DirectRead(localChange.BlockOffset, buffer, 0);
                await _serviceClient.PutAsync(new PushBlock()
                    {
                        DiskName = diskModel.Name,
                        BlockOffset = localChange.BlockOffset.Offset,
                        Data = buffer
                    });
                blocksProcessed++;
            }
        }

        private async Task _mergeRemoteBlockAsync(DiskViewModel diskModel, BlockOffset blockOffset, byte[] buffer)
        {
            using (var blockResp = await _serviceClient.GetAsync(new GetBlock()
                {
                    DiskName = diskModel.Name,
                    BlockOffset = blockOffset.Offset
                }))
            {
                // incorporate into the local disk
                blockResp.Read(buffer, 0, buffer.Length);
                diskModel.SynchronizingDisk.ReceiveChanges(blockOffset, buffer);
            }
        }

        private static int _uniqueDiskNameCounter;

        private class WindowsDispatcherAdapter : INotificationDispatcher
        {
            [NotNull]
            private readonly Dispatcher _dispatcher;

            public WindowsDispatcherAdapter(Dispatcher dispatcher)
            {
                _dispatcher = dispatcher;
            }

            public bool CheckAccess()
            {
                return _dispatcher.CheckAccess();
            }

            public Task BeginInvoke(Delegate method, params object[] args)
            {
                return _dispatcher.BeginInvoke(method, DispatcherPriority.Background, args).Task;
            }
        }

        public void Disconnect()
        {
            var client = Interlocked.Exchange(ref _serviceClient, null);
            if (client != null)
                client.Dispose();
            StatusText = "Disconnected. Changes will be recorded.";
            OnPropertyChanged("IsConnected");
            OnPropertyChanged("CanConnect");
        }

        public bool CanSynchronize(DiskViewModel diskModel)
        {
            return diskModel != null && IsConnected && diskModel.SynchronizingDisk.ServerAssociation != null;
        }
    }
}
