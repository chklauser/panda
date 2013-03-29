using Panda.Core;
using Panda.Core.Blocks;
using Panda.Core.Internal;
using Panda.Test.InMemory.Blocks;

namespace Panda.Test.Unit
{
    public class FileSystemBlackBox
    {
        public VirtualDisk Disk { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage ("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "In unit testing code, read-ability is more important than the worry that a hypothetical non-C#, non-VB.NET, non-F# client of the *TESTING* assembly might have to supply a handful of parameters.")]
        public void CreateMemDisk(uint totalBlockCount = 256, int blockCapacity = 16, int dataBlockCapcity = 128)
        {
            Disk = new VirtualDiskImpl(
                SingleInstanceMemBlockManager.Create(totalBlockCount, (BlockOffset) 1, blockCapacity, dataBlockCapcity), 
                new AscendingOffsetLockingPolicy());
        }
    }
}