using System.Collections.Generic;
using Panda.ServiceModel;
using ServiceStack.ServiceInterface;

namespace Panda.Server.ServiceInterface
{
    public class DiskService : Service
    {
         public DisksResponse Get(Disks disks)
         {
             var authSession = this.GetSession();
             return new DisksResponse {DiskRecords = new List<DiskRecord>()};
         }
    }
}