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
    [Route("/disks/{DiskName}/{BlockOffset}", Verbs = "POST")]
    public class PushBlock : IRequiresRequestStream
    {
        public String DiskName { get; set; }
        public long BlockOffset { get; set; }
        public DateTime DateChanged { get; set; }
        public Stream RequestStream { get; set; }
    }
}
