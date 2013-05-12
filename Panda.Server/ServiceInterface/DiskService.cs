using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Panda.Server.Persistence;
using Panda.ServiceModel;
using ServiceStack.Common.Web;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;

// ReSharper disable UnusedParameter.Global

namespace Panda.Server.ServiceInterface
{
    [UsedImplicitly]
    public class DiskService : Service
    {
        private IDiskRepository _diskRepository;

        protected IDiskRepository DiskRepository
        {
            get { return _diskRepository ?? (_diskRepository = ResolveService<IDiskRepository>()); }
        }

        [UsedImplicitly]
        public Stream Any(DownloadDisk downloadRequest)
        {
            using (var lease = DiskRepository[downloadRequest.DiskName])
            {
                lease.TemporarilyCloseDisk();

                // Since at the end of the lease, the disk could be re-acquired, we
                // need to copy the disk
                var tmp = Path.GetTempFileName();
                File.Copy(lease.DiskPath,tmp,true);
                return new FileStream(tmp, FileMode.Open, FileAccess.Read, FileShare.None,
                    81*1024, FileOptions.DeleteOnClose | FileOptions.SequentialScan);
            }
        }

        [UsedImplicitly]
        public DiskRecord Any(UploadDisk uploadDiskRequest)
        {
            using (var req = uploadDiskRequest.RequestStream)
            {
                var path = base.Request.PathInfo;
                var sidx = path.LastIndexOf('/');
                if(sidx < 0)
                    throw new ArgumentException("Missing file name.");
                uploadDiskRequest.DiskName = path.Substring(sidx + 1);

                var fileName = uploadDiskRequest.DiskName;
                if (!fileName.EndsWith(".panda"))
                    fileName += ".panda";

                try
                {
                    using (var fs = new FileStream(fileName, FileMode.CreateNew, FileAccess.Write))
                    {
                        req.CopyTo(fs);
                    }
                    using (var lease = DiskRepository[uploadDiskRequest.DiskName])
                    {
                        return _createDiskRecord<DiskRecord>(uploadDiskRequest.DiskName, lease.Disk);
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceError(e.ToString());
                    throw;
                }
            }
        }

        [UsedImplicitly]
        public GetDiskInfoResponse Any(GetDiskInfo getDiskInfoRequest)
        {
            try
            {
                using (var lease = DiskRepository[getDiskInfoRequest.DiskName])
                {
                    return _createDiskRecord<GetDiskInfoResponse>(getDiskInfoRequest.DiskName, lease.Disk);
                }
            }
            catch (FileNotFoundException)
            {
                throw HttpError.NotFound(string.Format("Cannot find disk named {0}.", getDiskInfoRequest.DiskName));
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                throw;
            }
        }

        [UsedImplicitly]
        public List<DiskRecord> Get(GetDisks getDisksRequest)
        {
            return DiskRepository.GetKnownDiskNames().AsParallel().Select(knownDiskName =>
                {
                    using (var lease = DiskRepository[knownDiskName])
                    {
                        var disk = lease.Disk;
                        return _createDiskRecord<DiskRecord>(knownDiskName, disk);
                    }
                }).ToList();
        }

        private static T _createDiskRecord<T>(string knownDiskName, VirtualDisk disk) where T:DiskRecord,new()
        {
            return new T
                {
                    Name = knownDiskName,
                    Capacity = disk.Capacity,
                    LastUpdated = disk.LastTimeSynchronized
                };
        }
    }
}