using System.Collections.Generic;
using System.IO;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace Panda.ServiceModel
{
    [Route("/disks/{DiskName}/batch/{BlockOffsets*}", Verbs = "GET")]
    [Route("/disks/{DiskName}/{BlockOffset}", Verbs = "GET")]
    public class GetBlock : IReturn<Stream>
    {
        public string DiskName { get; set; }
        public long BlockOffset { get; set; }
        public List<long> BatchBlockOffsets { get; set; }
    }
}