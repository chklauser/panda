using Panda.Core.Blocks;

namespace Panda.Core.Internal
{
    internal class VirtualDiskImpl : VirtualDisk
    {
        private readonly IBlockManager _blockManager;

        public VirtualDiskImpl(IBlockManager blockManager)
        {
            _blockManager = blockManager;
        }

        public override long Capacity
        {
            get { throw new System.NotImplementedException(); }
        }

        public override long CurrentSize
        {
            get { throw new System.NotImplementedException(); }
        }

        public override long FreeSpace
        {
            get { throw new System.NotImplementedException(); }
        }

        public override VirtualDirectory Root
        {
            get { throw new System.NotImplementedException(); }
        }

        protected IBlockManager BlockManager
        {
            get { return _blockManager; }
        }
    }
}