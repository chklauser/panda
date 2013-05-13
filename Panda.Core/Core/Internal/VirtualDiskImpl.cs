using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Panda.Core.Blocks;
using Panda.Core.IO;

namespace Panda.Core.Internal
{
    public abstract class VirtualDiskImpl : VirtualDisk, ISynchronizingDisk
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
            get { return _blockManager.TotalBlockCount*_blockManager.BlockSize; }
        }

        public override long CurrentSize
        {
            get { return Root.Size; }
        }

        public override long FreeSpace
        {
            get { return (_blockManager.TotalBlockCount-_blockManager.TotalFreeBlockCount)*_blockManager.BlockSize; }
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

        public override DateTime LastTimeSynchronized
        {
            get { return _blockManager.LastTimeSynchronized; }
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

        #region Disk synchronization

        public virtual IEnumerable<JournalEntry> GetJournalEntriesSince(DateTime lastSynchronization)
        {
            var raw = BlockManager as RawBlockManager;
            if (raw == null)
                throw new NotSupportedException(
                    "The block manager underlying this virtual disk does not support synchronization.");

            return raw.GetJournalEntriesSince(lastSynchronization);
        }

        public virtual void ReceiveChanges(BlockOffset blockOffset, byte[] data)
        {
            var raw = BlockManager as RawBlockManager;
            if (raw == null)
                throw new NotSupportedException(
                    "The block manager underlying this virtual disk does not support synchronization.");

            raw.WriteBlockDirect(blockOffset, data);
        }

        public virtual void DirectRead(BlockOffset blockOffset, byte[] destination, int index)
        {
            var raw = BlockManager as RawBlockManager;
            if (raw == null)
                throw new NotSupportedException(
                    "The block manager underlying this virtual disk does not support synchronization.");

            raw.ReadDataBlock(blockOffset, destination, index,0,null);
        }

        public string ServerAssociation
        {
            get
            {
                var raw = BlockManager as RawBlockManager;
                if (raw == null)
                    throw new NotSupportedException(
                        "The block manager underlying this virtual disk does not support synchronization.");

                return raw.ServerDiskName;
            }
            set
            {
                var raw = BlockManager as RawBlockManager;
                if (raw == null)
                    throw new NotSupportedException(
                        "The block manager underlying this virtual disk does not support synchronization.");

                raw.ServerDiskName = value;
            }
        }

        public void NotifySynchronized()
        {
            var raw = BlockManager as RawBlockManager;
            if (raw == null)
                throw new NotSupportedException(
                    "The block manager underlying this virtual disk does not support synchronization.");

            raw.NotifySynchronized();
        }

        public int BlockSize
        {
            get
            {
                var raw = BlockManager as RawBlockManager;
                if (raw == null)
                    throw new NotSupportedException(
                        "The block manager underlying this virtual disk does not support synchronization.");

                return raw.BlockSize;
            }
        }

        #endregion

    }
}