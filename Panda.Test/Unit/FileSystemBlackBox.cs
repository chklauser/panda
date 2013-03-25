using Panda.Core;
using Panda.Core.Blocks;
using Panda.Core.Internal;
using Panda.Test.InMemory.Blocks;

namespace Panda.Test.Unit
{
    public class FileSystemBlackBox
    {
        public VirtualDisk Disk;

        public void CreateMemDisk(uint totalBlockCount = 256, int blockCapacity = 16, int dataBlockCapcity = 128)
        {
            Disk = new VirtualDiskImpl(
                new MemBlockManager(totalBlockCount, (BlockOffset) 1, blockCapacity, dataBlockCapcity), 
                new AscendingOffsetLockingPolicy());
        }
    }
}