using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace Panda.ServiceModel
{
    [Route("/disks",Verbs = "GET")]
    [Authenticate]
    public class Disks : IReturn<DisksResponse>
    {
    }

    public class DisksResponse
    {
        public List<DiskRecord> DiskRecords { get; set; }
    }

    public class DiskRecord
    {
        public string Name { get; set; }
        public long Capacity { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}