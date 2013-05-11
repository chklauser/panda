using System;
using System.Threading;
using JetBrains.Annotations;
using Panda.Core.Blocks;
using Panda.Core.Internal;

namespace Panda.Core.IO
{
    public class RawBlock : IBlock, IDisposable
    {
        private readonly RawBlockManager _manager;
        private readonly BlockOffset _offset;
        private readonly uint _blockSize;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public RawBlock([NotNull] RawBlockManager manager, BlockOffset offset, uint blockSize)
        {
            _manager = manager;
            _offset = offset;
            _blockSize = blockSize;
        }

        public IRawPersistenceSpace Space
        {
            get { return _manager.Space; }
        }

        public BlockOffset Offset
        {
            get { return _offset; }
        }

        public unsafe void* ThisPointer
        {
            get { return ((byte*) Space.Pointer) + BlockSize*Offset.Offset; }
        }

        public ReaderWriterLockSlim Lock
        {
            get { return _lock; }
        }

        BlockOffset ICacheKeyed<BlockOffset>.CacheKey
        {
            get { return Offset; }
        }

        public uint BlockSize
        {
            get { return _blockSize; }
        }

        protected void OnBlockChanged()
        {
            _manager.OnBlockChanged(Offset);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _lock.Dispose();
            }
        }

        ~RawBlock()
        {
            Dispose(false);
        }
    }
}