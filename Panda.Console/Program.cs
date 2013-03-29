using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Panda.Core.Internal;

namespace Panda.Console
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (args == null)
                throw new ArgumentNullException("args");
            args.Ignore();
        }
    }
}
