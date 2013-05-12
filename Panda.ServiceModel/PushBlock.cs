using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace Panda.ServiceModel
{
    [Route("/disks/{DiskName}/{BlockOffset}", Verbs = "PUT,POST")]
    public class PushBlock
    {
        public String DiskName { get; set; }
        public long BlockOffset { get; set; }
        public byte[] Data { get; set; }
    }
}
