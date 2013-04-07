using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Panda.UI.ViewModel
{
    public class BrowserViewModel : IDisposable, INotifyPropertyChanged
    {
        private readonly ObservableCollection<DiskViewModel> _openDisks = new ObservableCollection<DiskViewModel>();
        private string _statusText;

        public BrowserViewModel()
        {
            _openDisks.CollectionChanged += _openDisks_CollectionChanged;
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
    }
}
