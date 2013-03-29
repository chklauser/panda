
using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using Panda.Core.Blocks;
using Panda.Core.Internal;

//13
namespace Panda.Core.IO
{
    public abstract class SingleInstanceRawBlockManager : Panda.Core.IO.RawBlockManager, IDisposable
    {
        [NotNull]
        protected abstract IReferenceCache<IBlock> ReferenceCache { get; }

        #region Default implementation

        [NotNull]
        public static SingleInstanceRawBlockManager Create(IRawPersistenceSpace persistenceSpace)
        {
            return new Default(persistenceSpace);
        }

		public SingleInstanceRawBlockManager(IRawPersistenceSpace persistenceSpace) : base(persistenceSpace)
		{
		}

        private class Default : SingleInstanceRawBlockManager
        {
            [NotNull]
            private readonly IReferenceCache<IBlock> _referenceCache = new LastAccessCache<BlockOffset, IBlock>(512);

            public Default(IRawPersistenceSpace persistenceSpace) : base(persistenceSpace)
            {
            }

            protected override IReferenceCache<IBlock> ReferenceCache
            {
                get { return _referenceCache; }
            }
        }

        #endregion

        private readonly Dictionary<BlockOffset, WeakReference<IBlock>> _existingBlocks =
            new Dictionary<BlockOffset, WeakReference<IBlock>>();

        private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

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

        public override IDirectoryBlock AllocateDirectoryBlock()
        {
            _handleGc();
            var block = base.AllocateDirectoryBlock();
            TrackNewBlock(block);
            return block;
        }

        public override IDirectoryContinuationBlock AllocateDirectoryContinuationBlock()
        {
            _handleGc();
            var block = base.AllocateDirectoryBlock();
            TrackNewBlock(block);
            return block;
        }

        public override IFileBlock AllocateFileBlock()
        {
            _handleGc();
            var block = base.AllocateFileBlock();
            TrackNewBlock(block);
            return block;
        }

        public override IFileContinuationBlock AllocateFileContinuationBlock()
        {
            _handleGc();
            var block = base.AllocateFileContinuationBlock();
            TrackNewBlock(block);
            return block;
        }

        public override void FreeBlock(BlockOffset blockOffset)
        {
            base.FreeBlock(blockOffset);

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

        public override IDirectoryBlock GetDirectoryBlock(BlockOffset blockOffset)
        {
            _handleGc();
			_lock.Wait();
			try
			{
				IDirectoryBlock block;
				if (!_tryGetExisting(blockOffset, out block))
				{
					block = base.GetDirectoryBlock(blockOffset);
					_trackNewBlock(block);
				}
				return block;
			}
			finally
			{
				_lock.Release();
			}
        }

        public override IDirectoryContinuationBlock GetDirectoryContinuationBlock(BlockOffset blockOffset)
        {
            _handleGc();
            _lock.Wait();
			try
			{
				IDirectoryContinuationBlock block;
				if (!_tryGetExisting(blockOffset, out block))
				{
					block = base.GetDirectoryContinuationBlock(blockOffset);
					_trackNewBlock(block);
				}
				return block;
			}
			finally
			{
				_lock.Release();
			}
        }

        public override IFileBlock GetFileBlock(BlockOffset blockOffset)
        {
            _handleGc();
            
            _lock.Wait();
			try
			{
				IFileBlock block;
				if (!_tryGetExisting(blockOffset, out block))
				{
					block = base.GetFileBlock(blockOffset);
					_trackNewBlock(block);
				}
				return block;
			}
			finally
			{
				_lock.Release();
			}
        }

        public override IFileContinuationBlock GetFileContinuationBlock(BlockOffset blockOffset)
        {
            _handleGc();
            _lock.Wait();
			try
			{
				IFileContinuationBlock block;
				if (!_tryGetExisting(blockOffset, out block))
				{
					block = base.GetFileContinuationBlock(blockOffset);
					_trackNewBlock(block);
				}
				return block;
			}
			finally
			{
				_lock.Release();
			}
        }

		#region IDisposable

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if(disposing)
			{
				var old = _lock;
				if(old != null && Interlocked.CompareExchange(ref _lock,old,null) == old)
				{
					old.Dispose();
				}
			}
		}

		~SingleInstanceRawBlockManager()
		{
			Dispose(false);
		}

		#endregion
    }
}

//26
namespace Panda.Test.InMemory.Blocks
{
    public abstract class SingleInstanceMemBlockManager : Panda.Test.InMemory.Blocks.MemBlockManager, IDisposable
    {
        [NotNull]
        protected abstract IReferenceCache<IBlock> ReferenceCache { get; }

        #region Default implementation

        [NotNull]
        public static SingleInstanceMemBlockManager Create(uint totalBlockCount, BlockOffset rootDirectoryBlockOffset, int metaBlockCapacity, int dataBlockCapacity)
        {
            return new Default(totalBlockCount, rootDirectoryBlockOffset, metaBlockCapacity, dataBlockCapacity);
        }

		public SingleInstanceMemBlockManager(uint totalBlockCount, BlockOffset rootDirectoryBlockOffset, int metaBlockCapacity, int dataBlockCapacity) : base(totalBlockCount, rootDirectoryBlockOffset, metaBlockCapacity, dataBlockCapacity)
		{
		}

        private class Default : SingleInstanceMemBlockManager
        {
            [NotNull]
            private readonly IReferenceCache<IBlock> _referenceCache = new LastAccessCache<BlockOffset, IBlock>(512);

            public Default(uint totalBlockCount, BlockOffset rootDirectoryBlockOffset, int metaBlockCapacity, int dataBlockCapacity) : base(totalBlockCount, rootDirectoryBlockOffset, metaBlockCapacity, dataBlockCapacity)
            {
            }

            protected override IReferenceCache<IBlock> ReferenceCache
            {
                get { return _referenceCache; }
            }
        }

        #endregion

        private readonly Dictionary<BlockOffset, WeakReference<IBlock>> _existingBlocks =
            new Dictionary<BlockOffset, WeakReference<IBlock>>();

        private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

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

        public override IDirectoryBlock AllocateDirectoryBlock()
        {
            _handleGc();
            var block = base.AllocateDirectoryBlock();
            TrackNewBlock(block);
            return block;
        }

        public override IDirectoryContinuationBlock AllocateDirectoryContinuationBlock()
        {
            _handleGc();
            var block = base.AllocateDirectoryBlock();
            TrackNewBlock(block);
            return block;
        }

        public override IFileBlock AllocateFileBlock()
        {
            _handleGc();
            var block = base.AllocateFileBlock();
            TrackNewBlock(block);
            return block;
        }

        public override IFileContinuationBlock AllocateFileContinuationBlock()
        {
            _handleGc();
            var block = base.AllocateFileContinuationBlock();
            TrackNewBlock(block);
            return block;
        }

        public override void FreeBlock(BlockOffset blockOffset)
        {
            base.FreeBlock(blockOffset);

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

        public override IDirectoryBlock GetDirectoryBlock(BlockOffset blockOffset)
        {
            _handleGc();
			_lock.Wait();
			try
			{
				IDirectoryBlock block;
				if (!_tryGetExisting(blockOffset, out block))
				{
					block = base.GetDirectoryBlock(blockOffset);
					_trackNewBlock(block);
				}
				return block;
			}
			finally
			{
				_lock.Release();
			}
        }

        public override IDirectoryContinuationBlock GetDirectoryContinuationBlock(BlockOffset blockOffset)
        {
            _handleGc();
            _lock.Wait();
			try
			{
				IDirectoryContinuationBlock block;
				if (!_tryGetExisting(blockOffset, out block))
				{
					block = base.GetDirectoryContinuationBlock(blockOffset);
					_trackNewBlock(block);
				}
				return block;
			}
			finally
			{
				_lock.Release();
			}
        }

        public override IFileBlock GetFileBlock(BlockOffset blockOffset)
        {
            _handleGc();
            
            _lock.Wait();
			try
			{
				IFileBlock block;
				if (!_tryGetExisting(blockOffset, out block))
				{
					block = base.GetFileBlock(blockOffset);
					_trackNewBlock(block);
				}
				return block;
			}
			finally
			{
				_lock.Release();
			}
        }

        public override IFileContinuationBlock GetFileContinuationBlock(BlockOffset blockOffset)
        {
            _handleGc();
            _lock.Wait();
			try
			{
				IFileContinuationBlock block;
				if (!_tryGetExisting(blockOffset, out block))
				{
					block = base.GetFileContinuationBlock(blockOffset);
					_trackNewBlock(block);
				}
				return block;
			}
			finally
			{
				_lock.Release();
			}
        }

		#region IDisposable

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if(disposing)
			{
				var old = _lock;
				if(old != null && Interlocked.CompareExchange(ref _lock,old,null) == old)
				{
					old.Dispose();
				}
			}
		}

		~SingleInstanceMemBlockManager()
		{
			Dispose(false);
		}

		#endregion
    }
}


