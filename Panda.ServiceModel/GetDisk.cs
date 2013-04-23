using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace Panda.ServiceModel
{
    [Route("/disks/{Name}",Verbs = "GET,HEADER")]
    [Authenticate]
    public class GetDisk : IReturn<DiskRecord>
    {
        
    }
}