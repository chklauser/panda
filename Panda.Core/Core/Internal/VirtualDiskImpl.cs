using JetBrains.Annotations;
using Panda.Core.Blocks;

namespace Panda.Core.Internal
{
    internal class VirtualDiskImpl : VirtualDisk
    {
        [NotNull] private readonly IBlockManager _blockManager;
        [NotNull] private readonly ILockingPolicy _lockingPolicy;

        public VirtualDiskImpl([NotNull] IBlockManager blockManager, [NotNull] ILockingPolicy lockingPolicy)
        {
            _blockManager = blockManager;
            _lockingPolicy = lockingPolicy;
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
            get { return new VirtualDirectoryImpl(this, _blockManager.RootDirectoryBlockOffset, ""); }
        }

        [NotNull]
        public IBlockManager BlockManager
        {
            get { return _blockManager; }
        }

        [NotNull]
        public ILockingPolicy LockingPolicy
        {
            get { return _lockingPolicy; }
        }
    }
}