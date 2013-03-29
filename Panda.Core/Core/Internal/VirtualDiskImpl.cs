using System;
using JetBrains.Annotations;
using Panda.Core.Blocks;

namespace Panda.Core.Internal
{
    internal class VirtualDiskImpl : VirtualDisk
    {
        [NotNull] private readonly IBlockManager _blockManager;
        [NotNull] private readonly ILockingPolicy _lockingPolicy;
        [NotNull] private readonly VirtualRootDirectoryImpl _rootDirectory;

        public VirtualDiskImpl([NotNull] IBlockManager blockManager, [NotNull] ILockingPolicy lockingPolicy)
        {
            _blockManager = blockManager;
            _lockingPolicy = lockingPolicy;
            _rootDirectory = new VirtualRootDirectoryImpl(this);
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
            get { return _rootDirectory; }
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