using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Panda.UI.ViewModel
{
    public class DiskViewModel : INotifyPropertyChanged
    {
        private string _name;
        private VirtualDisk _disk;
        private string _fileName;

        [NotNull]
        public string Name
        {
            get { return _name; }
            set
            {
                if (value == _name) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        [NotNull]
        public VirtualDisk Disk
        {
            get { return _disk; }
            set
            {
                if (Equals(value, _disk)) return;
                _disk = value;
                OnPropertyChanged();
                OnPropertyChanged("SynchronizingDisk");
            }
        }

        [NotNull]
        public string FileName
        {
            get { return _fileName; }
            set
            {
                if (value == _fileName) return;
                _fileName = value;
                OnPropertyChanged();
            }
        }

        [NotNull]
        public ISynchronizingDisk SynchronizingDisk { get { return (ISynchronizingDisk) Disk; } }

        public bool CanDisconnect
        {
            get { return SynchronizingDisk.ServerAssociation != null; }
        }

        public void Disconnect()
        {
            SynchronizingDisk.ServerAssociation = null;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Since we update the disk at a level way below the file system, we cannot know for sure
        /// which files and directories are affected by the synchronization.
        /// Therefore, we have to pessimistically assume that the entire disk has changed radically.
        /// </summary>
        public void NotifyDiskChangedExternally()
        {
            Disk.Root.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnPropertyChanged("Disk");
            OnPropertyChanged("SynchronizingDisk");
        }
    }
}