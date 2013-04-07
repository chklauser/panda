using System;
using JetBrains.Annotations;
using Panda.Core.Blocks;

namespace Panda.Core.Internal
{
    public abstract class VirtualDiskImpl : VirtualDisk
    {
        [NotNull] private readonly IBlockManager _blockManager;
        [NotNull] private readonly ILockingPolicy _lockingPolicy;
        [NotNull] private readonly VirtualRootDirectoryImpl _rootDirectory;

        internal VirtualDiskImpl([NotNull] IBlockManager blockManager, [NotNull] ILockingPolicy lockingPolicy)
        {
            _blockManager = blockManager;
            _lockingPolicy = lockingPolicy;
            _rootDirectory = new VirtualRootDirectoryImpl(this);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                var bm = _blockManager as IDisposable;
                if (bm != null)
                {
                    bm.Dispose();
                }
            }
        }

        public override long Capacity
        {
            get { return _blockManager.TotalBlockCount*_blockManager.DataBlockSize; }
        }

        public override long CurrentSize
        {
            get { return Root.Size; }
        }

        public override long FreeSpace
        {
            get { return (_blockManager.TotalBlockCount-_blockManager.TotalFreeBlockCount)*_blockManager.DataBlockSize; }
        }

        public override VirtualDirectory Root
        {
            get { return _rootDirectory; }
        }

        [NotNull]
        internal IBlockManager BlockManager
        {
            get { return _blockManager; }
        }

        [NotNull]
        internal ILockingPolicy LockingPolicy
        {
            get { return _lockingPolicy; }
        }

        internal virtual VirtualDirectoryImpl GetDirectory(VirtualDirectoryImpl parentDirectory, DirectoryEntry de)
        {
            return new VirtualDirectoryImpl(this, de.BlockOffset, parentDirectory, de.Name);
        }

        internal virtual VirtualFileImpl GetFile(VirtualDirectoryImpl parentDirectory, DirectoryEntry de)
        {
            return new VirtualFileImpl(this, de.BlockOffset, parentDirectory, de.Name);
        }

        protected internal virtual void OnDelete(ICacheKeyed<BlockOffset> node)
        {
        }

        internal VirtualNode GetNode(VirtualDirectoryImpl parentDirectory, DirectoryEntry de)
        {
            if (de.IsDirectory)
            {
                return GetDirectory(parentDirectory, de);
            }
            else
            {
                return GetFile(parentDirectory, de);
            }
        }
    }
}