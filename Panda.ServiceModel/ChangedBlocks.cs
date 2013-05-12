using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;

namespace Panda.ServiceModel
{
    [Route("/disks/{DiskName}/changes")]
    public class ChangedBlocks : IReturn<ChangedBlocksResponse>
    {
        public string DiskName { get; set; }
        public DateTime ChangesSince { get; set; }
    }

    public class ChangedBlocksResponse
    {
        public String DiskName { get; set; }
        public DateTime ChangesSince{ get; set; }
        public DateTime LastUpdated { get; set; }
        public List<ChangeRecord> Changes { get; set; }
    }

    public class ChangeRecord
    {
        public DateTime DateChanged { get; set; }
        public long BlockOffset { get; set; }
    }
}