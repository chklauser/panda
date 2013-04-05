using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Panda.Core.Blocks;
using Panda.Core.IO;
using Panda.Core.IO.InMemory;

namespace Panda.Test.Unit
{
    [TestFixture]
    public class RawBlockManagerMemTests : RawBlockManagerTestsBase
    {

        protected override IRawPersistenceSpace InstantiateSpace(uint blockCount, uint blockSize)
        {
            return new InMemorySpace(blockCount*blockSize);
        }
    }
}
