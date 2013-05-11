using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Panda.Server.Persistence
{
    public class DiskRepository : IDisposable, IDiskRepository
    {
        private readonly ConcurrentDictionary<string, DiskHandler> _handlers =
            new ConcurrentDictionary<string, DiskHandler>(StringComparer.OrdinalIgnoreCase);

        public DiskLease this[string diskName]
        {
            get
            {
                return new DiskLease(_handlers.GetOrAdd(diskName, dn => new DiskHandler(dn)));
            }
        }

        public IEnumerable<string> GetKnownDiskNames()
        {
            return Directory.EnumerateFiles(Environment.CurrentDirectory, "*.panda", SearchOption.TopDirectoryOnly);
        }

        #region Disposal

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var diskHandler in _handlers.Values)
                    diskHandler.Dispose();
            }
        }

        ~DiskRepository()
        {
            Dispose(false);
        }

        #endregion

    }
}