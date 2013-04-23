using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.DataAnnotations;

namespace Panda.Server.Persistence
{
    public class DiskAssociation
    {
        [AutoIncrement,PrimaryKey]
        public int Id { get; set; }

        [Index(Unique = true)]
        public String DiskName { get; set; }

        public String UserAuthId { get; set; }
        public long Capacity { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
