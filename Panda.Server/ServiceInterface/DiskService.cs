using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Panda.Server.Persistence;
using Panda.ServiceModel;
using ServiceStack.ServiceInterface;

namespace Panda.Server.ServiceInterface
{
    public class DiskService : Service
    {
        private IDiskRepository _diskRepository;

        public IDiskRepository DiskRepository
        {
            get
            {
                if (_diskRepository == null)
                {
                    _diskRepository = ResolveService<IDiskRepository>();
                }
                return _diskRepository;
            }
        }

        public DisksResponse Get(Disks disks)
        {
            var records = DiskRepository.GetKnownDiskNames().AsParallel().Select(knownDiskName =>
                {
                    using (var lease = DiskRepository[knownDiskName])
                    {
                        var disk = lease.Disk;
                        return new DiskRecord
                            {
                                Name = knownDiskName,
                                Capacity = disk.Capacity,
                                LastUpdated = disk.LastTimeSynchronized
                            };
                    }
                });

            return new DisksResponse {DiskRecords = records.ToList()};
        }
    }
}