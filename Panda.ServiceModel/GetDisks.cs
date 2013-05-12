using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;

namespace Panda.ServiceModel
{
    [Route("/disks",Verbs = "GET")]
    public class GetDisks : IReturn<List<DiskRecord>>
    {
    }

    public class DiskRecord : IHasResponseStatus
    {
        public string Name { get; set; }
        public long Capacity { get; set; }
        public DateTime LastUpdated { get; set; }

        public ResponseStatus ResponseStatus { get; set; }
    }
}