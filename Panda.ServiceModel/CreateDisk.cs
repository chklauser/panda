using System;
using System.IO;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace Panda.ServiceModel
{
    [Route("/disks/{Name}", Verbs = "POST")]
    [Authenticate]
    public class CreateDisk : IReturn<DiskRecord>, IRequiresRequestStream
    {
        public string Name { get; set; }
        public long Capacity { get; set; }

        public Stream RequestStream {  get; set; }
    }
}