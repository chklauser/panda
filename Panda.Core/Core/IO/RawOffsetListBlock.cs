using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

    public class RawContinuedBlock : RawBlock, IContinuationBlock
    {
        public RawContinuedBlock(IRawPersistenceSpace space, BlockOffset offset, uint blockSize) : base(space, offset, blockSize)
        {
        }

        protected unsafe BlockOffset* ContinuationBlockSlot
        {
            get
            {
                return (BlockOffset*)(((byte*)ThisPointer) + BlockSize - sizeof(BlockOffset));
            }
        }

        public unsafe BlockOffset? ContinuationBlock
        {
            get
            {
                var bo = *ContinuationBlockSlot;
                if (bo.Offset == 0)
                    return null;
                else
                    return bo;
            }
            set
            {
                if (value == null)
                {
                    *ContinuationBlockSlot = (BlockOffset)0;
                }
                else
                {
                    *ContinuationBlockSlot = value.Value;
                }
            }
        }
    }

    public class RawOffsetListBlock : RawContinuedBlock, IEmptyListBlock, IFileContinuationBlock
    {
        public RawOffsetListBlock([NotNull] IRawPersistenceSpace space, BlockOffset offset, uint size)
            : base(space, offset, size)
        {
        }

        public IEnumerator<BlockOffset> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            // we don't actually have a better way to compute the count than to parse the block, offset by offset
            // just counting would be a bit more efficient than asking an enumerator, but Count is not
            // a property that will be used often.
// ReSharper disable InvokeAsExtensionMethod
            get { return Enumerable.Count(this); }
// ReSharper restore InvokeAsExtensionMethod
        }

        public unsafe int ListCapacity
        {
            get
            {
                // The number of offsets that fit into the block minus total offset count and continuation offset
                return (int) (BlockSize/sizeof (BlockOffset) - 2);
            }
        }

        public void ReplaceOffsets(BlockOffset[] offsets)
        {
            throw new NotImplementedException();
        }

        public unsafe int TotalFreeBlockCount
        {
            get { return (int) (*((uint*) ThisPointer)); }
        }

        public BlockOffset[] Remove(int count)
        {
            throw new NotImplementedException();
        }

        public void Append(BlockOffset[] freeBlockOffsets)
        {
            throw new NotImplementedException();
        }
    }
}
