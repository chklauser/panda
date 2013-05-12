using System;
using System.IO;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace Panda.ServiceModel
{
    [Route("/disks/{DiskName}", Verbs = "POST,PUT")]
    public class UploadDisk : IReturn<DiskRecord>, IRequiresRequestStream
    {
        public string DiskName { get; set; }
        public long Capacity { get; set; }

        public Stream RequestStream {  get; set; }
    }
}