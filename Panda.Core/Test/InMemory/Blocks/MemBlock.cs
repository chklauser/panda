using System.Collections.Generic;
using System.Threading;
using Panda.Core.Blocks;
using Panda.Core.Internal;

namespace Panda.Test.InMemory.Blocks
{
    public abstract class MemBlock : IContinuationBlock
    {
        private BlockOffset? _continuationBlock;
        private readonly BlockOffset _offset;
        public BlockOffset Offset
        {
            get { return _offset; }
        }

        public ReaderWriterLockSlim Lock { get; private set; }
        public bool IsAllocated { get; set; }

        public MemBlock(BlockOffset offset)
        {
            _offset = offset;
            IsAllocated = true;
        }

        protected void ThrowIfDeallocated()
        {
            if (!IsAllocated)
            {
                throw new BlockDeallocatedException(string.Format(
                    "The representation of the block at offset {0} as a {1} was accessed after it was freed.", 
                    Offset, GetType().Name));
            }
        }

        // This is not used by all sub blocks, but for a mock class, it won't hurt
        public BlockOffset? ContinuationBlockOffset
        {
            get
            {
                ThrowIfDeallocated();
                return _continuationBlock;
            }
            set
            {
                ThrowIfDeallocated();
                _continuationBlock = value;
            }
        }

        protected IEnumerator<T> GuardedEnumerator<T>(IEnumerable<T> sequence)
        {
            // Ordinarily, you would just do this:
            // return X.GetEnumerator();

            // But we want to detect de-allocation errors even during enumeration
            // Thus we check for deallocation before every element.
            foreach (var item in sequence)
            {
                ThrowIfDeallocated();
                yield return item;
            }
        }

        BlockOffset ICacheKeyed<BlockOffset>.CacheKey
        {
            get { return Offset; }
        }
    }
}