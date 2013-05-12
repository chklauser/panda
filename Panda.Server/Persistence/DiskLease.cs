using System;
using System.Threading;
using JetBrains.Annotations;

namespace Panda.Server.Persistence
{
    public class DiskLease : IDisposable
    {
        [NotNull]
        private readonly DiskHandler _handler;

        public DiskLease([NotNull] DiskHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");
            
            _handler = handler;
            Monitor.Enter(handler);
        }

        [NotNull]
        public VirtualDisk Disk
        {
            get { return _handler.Disk; }
        }

        public void TemporarilyCloseDisk()
        {
            _handler.TemporarilyCloseDisk();
        }

        public string DiskName
        {
            get { return _handler.DiskName; }
        }

        public string DiskPath
        {
            get { return _handler.DiskPath; }
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
                _handler.LastAccess = DateTime.Now;
                Monitor.Exit(_handler);
            }
        }

        ~DiskLease()
        {
            Dispose(false);
        }
    }
}