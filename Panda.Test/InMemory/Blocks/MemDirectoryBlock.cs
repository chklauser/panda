using Panda.Core.Blocks;

namespace Panda.Test.InMemory.Blocks
{
    public class MemDirectoryBlock : MemDirectoryBase, IDirectoryBlock
    {
        private long _totalSize;

        public MemDirectoryBlock(BlockOffset offset, int capacity) : base(offset,capacity)
        {
        }

        public long TotalSize
        {
            get
            {
                ThrowIfDeallocated();
                return _totalSize;
            }
            private set
            {
                ThrowIfDeallocated();
                _totalSize = value;
            }
        }
    }
}