using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using JetBrains.Annotations;
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
        private string _serverUrl;
        private IServiceClient _serviceClient;
        private readonly ObservableCollection<DiskRecord> _serverDiskRecords = new ObservableCollection<DiskRecord>();

        public BrowserViewModel()
        {
            _openDisks.CollectionChanged += _openDisksCollectionChanged;
            _serverUrl = Settings.Default.ServerUrl;
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
        [NotifyPropertyChangedInvocator]
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

        public string ServerUrl
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
            if(_serviceClient != null)
                _serviceClient.Dispose();

            _serviceClient = ServerUrl != null ? new JsonServiceClient(ServerUrl) : null;
            OnPropertyChanged("IsConnected");
            OnPropertyChanged("CanConnect");
        }

        public async Task ConnectAsync()
        {
            _resetServiceClient();
            await RefreshServerDisksAsync();
        }

        public async Task RefreshServerDisksAsync()
        {
            var resp = await _serviceClient.GetAsync(new GetDisks());
            StatusText = String.Format("Connected to server at {0}.", ServerUrl);
            ServerDiskRecords.Clear();
            foreach (var diskRecord in resp)
                ServerDiskRecords.Add(diskRecord);
        }

        public bool CanAssociateDisk(DiskViewModel diskView)
        {
            return IsConnected && diskView.SynchronizingDisk.ServerAssociation == null;
        }

        public async Task AssociateDiskAsync(DiskViewModel diskView)
        {
            DiskRecord resp;
            try
            {
                resp = await _serviceClient.GetAsync(new GetDiskInfo {DiskName = diskView.Name});
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

            if (resp == null)
            {
                // need to upload the disk first
                StatusText = String.Format("Disk {0} is being uploaded to the server. Please wait...", diskView.Name);
                try
                {
                    var capacity = diskView.Disk.Capacity;
                    diskView.SynchronizingDisk.ServerAssociation = diskView.Name;
                    diskView.SynchronizingDisk.NotifySynchronized();
                    diskView.Disk.Dispose();
// ReSharper disable AssignNullToNotNullAttribute
                    diskView.Disk = null;
// ReSharper restore AssignNullToNotNullAttribute
                    
                    using (var fs = new FileStream(diskView.FileName,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
                    {
                        var request = new UploadDisk
                            {
                                Capacity = capacity, DiskName = diskView.Name, RequestStream = fs
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
                        await RefreshServerDisksAsync();
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
            var str = await _serviceClient.GetAsync(new DownloadDisk{DiskName = diskRecord.Name});
            var fileName = diskRecord.Name + ".panda";
            StatusText = string.Format("Downloading disk {0}. Please wait...", diskRecord.Name);
            using (var fs = new FileStream(fileName,FileMode.CreateNew,FileAccess.Write))
            {
                await str.CopyToAsync(fs);
            }
            OpenDisk(fileName,dispatcher);
            StatusText = string.Format("Disk {0} downloaded successfully.", diskRecord.Name);
        }

        public bool CanDownloadDiks([NotNull] DiskRecord diskRecord)
        {
            return IsConnected && OpenDisks.All(diskViewModel => !diskRecord.Name.Equals(diskViewModel.Name));
        }

        public void OpenDisk(string fileName, Dispatcher dispatcher)
        {
            VirtualDisk vdisk = null;
            try
            {
                vdisk = VirtualDisk.OpenExisting(fileName);
                RegisterDisk(fileName, vdisk,dispatcher);
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

        public void RegisterDisk(string fileName, VirtualDisk vdisk, Dispatcher dispatcher)
        {
            var name = Path.GetFileNameWithoutExtension(fileName) ??
                "disk" + Interlocked.Increment(ref _uniqueDiskNameCounter);

            if (dispatcher != null)
            {
                // have the virtual disk dispatch its notifications on the UI thread.
                vdisk.NotificationDispatcher = new WindowsDispatcherAdapter(dispatcher);
            }

            var diskModel = new DiskViewModel
                {
                Disk = vdisk,
                Name = name,
                FileName = fileName
            };
            OpenDisks.Add(diskModel);
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
            if(client != null)
                client.Dispose();
            StatusText = "Disconnected. Changes will be recorded.";
            OnPropertyChanged("IsConnected");
            OnPropertyChanged("CanConnect");
        }
    }
}
