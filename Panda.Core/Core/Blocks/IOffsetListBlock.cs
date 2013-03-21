using System.Collections;
using System.Collections.Generic;

namespace Panda.Core.Blocks
{
    public interface IOffsetListBlock : IBlock, IReadOnlyCollection<int>, IContinuationBlock
    {
        int ListCapacity { get; }
    }
}