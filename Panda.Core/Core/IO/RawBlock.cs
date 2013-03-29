using System.Threading;
using JetBrains.Annotations;
using Panda.Core.Blocks;
using Panda.Core.Internal;

namespace Panda.Core.IO
{
    public class RawBlock : IBlock
    {
        private readonly IRawPersistenceSpace _space;
        private readonly BlockOffset _offset;
        private readonly uint _blockSize;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public RawBlock([NotNull] IRawPersistenceSpace space, BlockOffset offset, uint blockSize)
        {
            _space = space;
            _offset = offset;
            _blockSize = blockSize;
        }

        public IRawPersistenceSpace Space
        {
            get { return _space; }
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
    }
}