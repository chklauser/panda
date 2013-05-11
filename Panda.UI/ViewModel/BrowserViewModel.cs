using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Panda.ServiceModel;
using Panda.UI.Internal;
using Panda.UI.Properties;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceInterface.Auth;

namespace Panda.UI.ViewModel
{
    public class BrowserViewModel : IDisposable, INotifyPropertyChanged
    {
        private readonly ObservableCollection<DiskViewModel> _openDisks = new ObservableCollection<DiskViewModel>();
        private string _statusText;
        private string _serverUrl;
        private IServiceClient _serviceClient;
        private string _username;
        private readonly ObservableCollection<DiskRecord> _serverDiskRecords = new ObservableCollection<DiskRecord>();

        public BrowserViewModel()
        {
            _openDisks.CollectionChanged += _openDisks_CollectionChanged;
            _serverUrl = Settings.Default.ServerUrl;
            _username = Settings.Default.Username;
        }

        void _openDisks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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

        private void _resetServiceClient()
        {
            if(_serviceClient != null)
                _serviceClient.Dispose();

            _serviceClient = ServerUrl != null ? new JsonServiceClient(ServerUrl) : null;
            OnPropertyChanged("IsConnected");
        }

        public async Task ConnectAsync()
        {
            _resetServiceClient();
            var resp = await _serviceClient.SendAsync(new Disks());
            StatusText = String.Format("Connected to server at {0}.", ServerUrl);
            ServerDiskRecords.Clear();
            foreach (var diskRecord in resp.DiskRecords)
                ServerDiskRecords.Add(diskRecord);
        }

    }
}
