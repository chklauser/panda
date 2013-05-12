using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Panda.Server.Persistence
{
    public class DiskHandler : IDisposable
    {
        [NotNull]
        private readonly string _diskName;
        public DateTime LastAccess
        {
            get { return _lastAccess; }
            set { _lastAccess = value; }
        }

        private volatile VirtualDisk _disk;
        private DateTime _lastAccess;
        private readonly TimeSpan _inactivityThreshold = TimeSpan.FromMinutes(1);
        private readonly CancellationTokenSource _monitorCancellation = new CancellationTokenSource();

        public DiskHandler([NotNull] string diskName)
        {
            if (diskName == null)
                throw new ArgumentNullException("diskName");
            
            _diskName = diskName;
        }

        [NotNull]
        public VirtualDisk Disk
        {
            get
            {
                if (_disk == null)
                {
                    lock (this)
                    {
                        if (_disk == null)
                        {
                            _disk = _openDisk();
                        }
                    }
                }
                LastAccess = DateTime.Now;
                return _disk;
            }
        }

        [NotNull]
        public string DiskName
        {
            get { return _diskName; }
        }

        [NotNull]
        private VirtualDisk _openDisk()
        {
            var disk = VirtualDisk.OpenExisting(DiskPath);

            _monitorInactivity();
            return disk;
        }

        public string DiskPath
        {
            get
            {
                var name = DiskName;
                if (!File.Exists(name))
                    name = name + ".panda";
                return name;
            }
        }

        #region Inactivity monitor

        private void _monitorInactivity()
        {
            Task.Delay(_inactivityThreshold, _monitorCancellation.Token)
                .ContinueWith(_checkInactivity, _monitorCancellation.Token);
        }

        private void _checkInactivity(Task previous)
        {
            _monitorCancellation.Token.ThrowIfCancellationRequested();

            if (DateTime.Now - LastAccess < _inactivityThreshold)
            {
                lock (this)
                {
                    if (DateTime.Now - LastAccess < _inactivityThreshold)
                    {
                        if (_disk != null)
                        {
                            Trace.WriteLine(string.Format(
                                "The disk {0} has been inactive since {1}. Closing the disk.", DiskName, LastAccess));
                            _disk.Dispose();
                            _disk = null;
                            return;
                        }
                    }
                }
            }
            Trace.WriteLine(string.Format("Activity detected in Disk {0}. Checking back in {1}.", DiskName,
                _inactivityThreshold));
            _monitorInactivity();
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if(_monitorCancellation != null)
                    _monitorCancellation.Cancel(false);

#pragma warning disable 420 // the whole point of interlocked exchange is to be thread safe
                var disk = Interlocked.Exchange(ref _disk, null);
#pragma warning restore 420
                if (disk != null)
                    disk.Dispose();
            }
        }

        ~DiskHandler()
        {
            Dispose(false);
        }

        #endregion

        public void TemporarilyCloseDisk()
        {
            if (_disk != null)
            {
                lock (this)
                {
                    if (_disk != null)
                    {
                        _disk.Dispose();
                        _disk = null;
                    }
                }
            }
        }
    }
}