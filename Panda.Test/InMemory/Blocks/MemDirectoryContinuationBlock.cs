using Panda.Core.Blocks;

namespace Panda.Test.InMemory.Blocks
{
    public class MemDirectoryContinuationBlock : MemDirectoryBase, IDirectoryContinuationBlock
    {
        public MemDirectoryContinuationBlock(int offset, int capacity) : base(offset, capacity)
        {
        }
    }
}