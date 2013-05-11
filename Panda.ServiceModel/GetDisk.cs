using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace Panda.ServiceModel
{
    [Route("/disks/{Name}",Verbs = "GET,HEADER")]
    public class GetDisk : IReturn<DiskRecord>
    {
        
    }
}