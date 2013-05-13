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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is a ServiceStack request data transfer object. The property is being assigned by the ServiceStack framework.")]
        public IList<long> BatchBlockOffsets { get; set; }
    }
}