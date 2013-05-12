using System.IO;
using ServiceStack.ServiceHost;

namespace Panda.ServiceModel
{
    [Route("/disks/{DiskName}",Verbs = "GET")]
    public class DownloadDisk : IReturn<Stream>
    {
        public string DiskName { get; set; }
    }
}