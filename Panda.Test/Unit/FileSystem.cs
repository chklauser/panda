using NUnit.Framework;
using Panda.Core.Internal;
using Panda.Test.InMemory.Blocks;

namespace Panda.Test.Unit
{
    [TestFixture]
    public class FileSystem
    {
        public VirtualDisk Disk;
        public MemBlockManager BlockManager;

        [SetUp]
        public void SetUp()
        {
            
        }

        public void CreateMemDisk(int totalBlockCount = 256, int rootDirectoryBlockOffset = 1, int blockCapacity = 16)
        {
            Disk = new VirtualDiskImpl(BlockManager = new MemBlockManager(totalBlockCount,rootDirectoryBlockOffset,blockCapacity));
        }

        [Test]
        public void CreateDirectory()
        {
            // You can customize your dummy disk via optional parameters.
            // For this test, we won't need a large disk.
            CreateMemDisk(totalBlockCount:16);

            // Use the disk to create a directory 'd' in '/d'
            var dir = Disk.Root.CreateDirectory("d");

            Assert.That(dir,Is.Not.Null);

            Assert.That(dir.Count,Is.EqualTo(0),"directory should be empty");
            Assert.That(dir.Name,Is.EqualTo("d"),"directory name");
            Assert.That(dir.IsRoot,Is.False,"/d is not root");
            Assert.That(dir.ParentDirectory,Is.SameAs(Disk.Root),"/d should be child of /");
            Assert.That(dir.Size,Is.EqualTo(0),"directory should report size 0");

            // and so on and so forth.
        }
    }
}