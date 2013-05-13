using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Panda.Core.Blocks;
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
                Trace.TraceInformation("Download({0}) begin", downloadRequest.DiskName);
                lease.TemporarilyCloseDisk();

                // Since at the end of the lease, the disk could be re-acquired, we
                // need to copy the disk
                var tmp = Path.GetTempFileName();
                Trace.TraceInformation("Download({0}) copying to {1}",downloadRequest.DiskName,tmp);
                File.Copy(lease.DiskPath,tmp,true);
                Trace.TraceInformation("Download({0}) copied to {1}", downloadRequest.DiskName, tmp);
                return new FileStream(tmp, FileMode.Open, FileAccess.Read, FileShare.None,
                    81*1024, FileOptions.DeleteOnClose | FileOptions.SequentialScan);
            }
        }

        [UsedImplicitly]
        public DiskRecord Any(UploadDisk uploadDiskRequest)
        {
            using (var req = uploadDiskRequest.RequestStream)
            {
                Trace.TraceInformation("Upload({0}) begin", uploadDiskRequest.DiskName);
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
                    Trace.TraceInformation("Upload({0}) written {1}", uploadDiskRequest.DiskName,fileName);
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
                Trace.TraceInformation("GetDiskInfo({0}) begin", getDiskInfoRequest.DiskName);
                using (var lease = DiskRepository[getDiskInfoRequest.DiskName])
                {
                    Trace.TraceInformation("GetDiskInfo({0}) found", getDiskInfoRequest.DiskName);
                    return _createDiskRecord<GetDiskInfoResponse>(getDiskInfoRequest.DiskName, lease.Disk);
                }
            }
            catch (FileNotFoundException)
            {
                Trace.TraceWarning("GetDiskInfo({0}) not found", getDiskInfoRequest.DiskName);
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
            Trace.TraceInformation("GetDisks() begin");
            return DiskRepository.GetKnownDiskNames().AsParallel().Select(knownDiskName =>
                {
                    using (var lease = DiskRepository[knownDiskName])
                    {
                        var disk = lease.Disk;
                        return _createDiskRecord<DiskRecord>(knownDiskName, disk);
                    }
                }).ToList();
        }

        [UsedImplicitly]
        public ChangedBlocksResponse Any(ChangedBlocks changedBlocksRequest)
        {
            try
            {
                Trace.TraceInformation("ChangedBlocks({0}, {1}) begin", changedBlocksRequest.DiskName, changedBlocksRequest.ChangesSince);
                using (var lease = DiskRepository[changedBlocksRequest.DiskName])
                {
                    var disk = lease.Disk;
                    var sync = (ISynchronizingDisk) disk;
                    var journal = sync.GetJournalEntriesSince(changedBlocksRequest.ChangesSince);
                    return new ChangedBlocksResponse
                        {
                            Changes = journal.Select(_toChangeRecord).ToList(),
                            ChangesSince = changedBlocksRequest.ChangesSince,
                            DiskName = changedBlocksRequest.DiskName,
                            LastUpdated = disk.LastTimeSynchronized
                        };
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                throw;
            }
        }

        [UsedImplicitly]
        public PushBlockResponse Any(PushBlock pushRequest)
        {
            try
            {
                Trace.TraceInformation("PushBlock({0},{1},byte[{2}])", pushRequest.DiskName, pushRequest.BlockOffset, pushRequest.Data.Length);

                using (var lease = DiskRepository[pushRequest.DiskName])
                {
                    var disk = lease.Disk;
                    var sync = (ISynchronizingDisk)disk;
                    sync.ReceiveChanges((BlockOffset) pushRequest.BlockOffset,pushRequest.Data);
                    return _createDiskRecord<PushBlockResponse>(pushRequest.DiskName, disk);
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                throw;
            }
        }

        [UsedImplicitly]
        public Stream Any(GetBlock getBlockRequest)
        {
            try
            {
                Trace.TraceInformation("GetBlock({0},{1})", getBlockRequest.DiskName, getBlockRequest.BlockOffset);
                using (var lease = DiskRepository[getBlockRequest.DiskName])
                {
                    var disk = lease.Disk;
                    var sync = (ISynchronizingDisk)disk;
                    var buffer = new byte[sync.BlockSize];
                    sync.DirectRead((BlockOffset) getBlockRequest.BlockOffset,buffer,0);
                    return new MemoryStream(buffer,0,buffer.Length,false);
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                throw;
            }
        }

        private ChangeRecord _toChangeRecord(JournalEntry je)
        {
            return new ChangeRecord {BlockOffset = je.BlockOffset.Offset, DateChanged = je.Date};
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