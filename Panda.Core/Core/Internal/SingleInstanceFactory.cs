using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using Panda.Core.Blocks;

namespace Panda.Core.Internal
{
    public abstract class SingleInstanceFactory : IBlockManager
    {
        [NotNull]
        protected abstract IBlockManager BackingManager { get; }

        [NotNull]
        protected abstract IBlockReferenceCache ReferenceCache { get; }

        #region Default implementation

        [NotNull]
        public static SingleInstanceFactory Create([NotNull] IBlockManager backingManager)
        {
            return new Default(backingManager);
        }

        private class Default : SingleInstanceFactory
        {
            [NotNull] private readonly IBlockManager _backingManager;

            [NotNull] private readonly IBlockReferenceCache _referenceCache = new LastAccessCache(512);

            public Default([NotNull] IBlockManager backingManager)
            {
                _backingManager = backingManager;
            }

            protected override IBlockManager BackingManager
            {
                get { return _backingManager; }
            }

            protected override IBlockReferenceCache ReferenceCache
            {
                get { return _referenceCache; }
            }
        }

        #endregion

        private readonly Dictionary<BlockOffset, WeakReference<IBlock>> _existingBlocks =
            new Dictionary<BlockOffset, WeakReference<IBlock>>();

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        protected SemaphoreSlim Lock
        {
            get { return _lock; }
        }

        private const int GcInterval = 2048;

        private void _trackNewBlock(IBlock block)
        {
            // assume exclusive access
            ReferenceCache.RegisterAccess(block);
            _existingBlocks.Add(block.Offset, new WeakReference<IBlock>(block));
        }

        protected void TrackNewBlock(IBlock block)
        {
            _lock.Wait();
            try
            {
                _trackNewBlock(block);
            }
            finally
            {
                _lock.Release();
            }
        }

        private uint _gcCounter;

        private void _handleGc()
        {
            // Assume exclusive access
            if (_gcCounter++ > GcInterval)
            {
                _gcCounter = 0;
                _collectGarbage();
            }
        }

        private void _collectGarbage()
        {
            // assume exclusive access
            var gced = new List<BlockOffset>();
            foreach (var weakRefEntry in _existingBlocks)
            {
                IBlock dummy;
                if (!weakRefEntry.Value.TryGetTarget(out dummy))
                {
                    gced.Add(weakRefEntry.Key);
                }
            }

            foreach (var offset in gced)
            {
                _existingBlocks.Remove(offset);
            }
        }

        [ContractAnnotation("=>true,block:notnull; =>false,block:null")]
        private bool _tryGetExisting<T>(BlockOffset offset, out T block) where T : class, IBlock
        {
            // assume exclusive access
            WeakReference<IBlock> weakBlock;
            IBlock anyBlock;
            if (_existingBlocks.TryGetValue(offset, out weakBlock)
                && weakBlock.TryGetTarget(out anyBlock))
            {
                block = (T) anyBlock;
                return true;
            }
            else
            {
                block = null;
                return false;
            }
        }

        [ContractAnnotation("=>true,block:notnull; =>false,block:null")]
        protected bool TryGetExisting<T>(BlockOffset offset, out T block) where T : class, IBlock
        {
            _lock.Wait();
            try
            {
                return _tryGetExisting(offset, out block);
            }
            finally
            {
                _lock.Release();
            }
        }

        public IDirectoryBlock AllocateDirectoryBlock()
        {
            _handleGc();
            var block = BackingManager.AllocateDirectoryBlock();
            TrackNewBlock(block);
            return block;
        }

        public IDirectoryContinuationBlock AllocateDirectoryContinuationBlock()
        {
            _handleGc();
            var block = BackingManager.AllocateDirectoryBlock();
            TrackNewBlock(block);
            return block;
        }

        public IFileBlock AllocateFileBlock()
        {
            _handleGc();
            var block = BackingManager.AllocateFileBlock();
            TrackNewBlock(block);
            return block;
        }

        public IFileContinuationBlock AllocateFileContinuationBlock()
        {
            _handleGc();
            var block = BackingManager.AllocateFileContinuationBlock();
            TrackNewBlock(block);
            return block;
        }

        public BlockOffset AllocateDataBlock()
        {
            return BackingManager.AllocateDataBlock();
        }

        public void FreeBlock(BlockOffset blockOffset)
        {
            BackingManager.FreeBlock(blockOffset);

            _lock.Wait();
            try
            {
                WeakReference<IBlock> weakBlock;
                IBlock block;
                if (_existingBlocks.TryGetValue(blockOffset, out weakBlock) && weakBlock.TryGetTarget(out block))
                {
                    ReferenceCache.EvictEarly(block);
                }
                _handleGc();
            }
            finally
            {
                _lock.Release();
            }
        }

        public IDirectoryBlock GetDirectoryBlock(BlockOffset blockOffset)
        {
            _handleGc();
            IDirectoryBlock block;
            if (!TryGetExisting(blockOffset, out block))
                throw new InvalidOperationException("There is no directory block at offset " + blockOffset);
            return block;
        }

        public IDirectoryContinuationBlock GetDirectoryContinuationBlock(BlockOffset blockOffset)
        {
            _handleGc();
            IDirectoryContinuationBlock block;
            if (!TryGetExisting(blockOffset, out block))
                throw new InvalidOperationException("There is no directory continuation block at offset " + blockOffset);
            return block;
        }

        public IFileBlock GetFileBlock(BlockOffset blockOffset)
        {
            _handleGc();
            IFileBlock block;
            if (!TryGetExisting(blockOffset, out block))
                throw new InvalidOperationException("There is no file block at offset " + blockOffset);
            return block;
        }

        public IFileContinuationBlock GetFileContinuationBlock(BlockOffset blockOffset)
        {
            _handleGc();
            IFileContinuationBlock block;
            if (!TryGetExisting(blockOffset, out block))
                throw new InvalidOperationException("There is no file continuation block at offset " + blockOffset);
            return block;
        }

        public void WriteDataBlock(BlockOffset blockOffset, byte[] data)
        {
            BackingManager.WriteDataBlock(blockOffset, data);
        }

        public void ReadDataBlock(BlockOffset blockOffset, byte[] destination, int destinationIndex = 0, int blockIndex = 0,
                                  int? count = null)
        {
            BackingManager.ReadDataBlock(blockOffset,destination,destinationIndex,blockIndex);
        }

        public int TotalBlockCount
        {
            get { return BackingManager.TotalBlockCount; }
        }

        public BlockOffset RootDirectoryBlockOffset
        {
            get { return BackingManager.RootDirectoryBlockOffset; }
        }

        public int DataBlockSize
        {
            get { return BackingManager.DataBlockSize; }
        }
    }
}