using Panda.Core.Blocks;

namespace Panda.Test.InMemory.Blocks
{
    public class MemFileBlock : MemOffsetList, IFileBlock
    {
        private long _size;

        public MemFileBlock(BlockOffset offset, int listCapacity) : base(offset, listCapacity)
        {
        }

        public long Size
        {
            get
            {
                ThrowIfDeallocated();
                return _size;
            }
            private set
            {
                ThrowIfDeallocated();
                _size = value;
            }
        }
    }
}