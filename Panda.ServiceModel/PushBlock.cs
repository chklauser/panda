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
    public class PushBlock : IReturn<PushBlockResponse>
    {
        public String DiskName { get; set; }
        public long BlockOffset { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays",Justification = "byte[] is a signal to the ServiceStack framework to expect binary data. Since this is just a data transfer object, the usual concerns about exposed internal arrays do not apply.")]
        public byte[] Data { get; set; }
    }

    public class PushBlockResponse : DiskRecord
    {
    }
}
