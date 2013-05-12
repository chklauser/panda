using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;

namespace Panda.ServiceModel
{
    [Route("/disks/{DiskName}", Verbs = "HEAD")]
    [Route("/disks/{DiskName}/meta", Verbs = "GET")]
    public class GetDiskInfo : IReturn<GetDiskInfoResponse>
    {
        public string DiskName { get; set; }
    }

    public class GetDiskInfoResponse : DiskRecord
    {
    }
}