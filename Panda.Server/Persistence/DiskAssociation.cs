using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panda.Server.Persistence
{
    public class DiskAssociation
    {
        public int Id { get; set; }
        public String DiskFileName { get; set; }
        public String UserAuthId { get; set; }
    }
}
