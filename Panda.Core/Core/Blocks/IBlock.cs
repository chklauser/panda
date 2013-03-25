using System.Threading;
using Panda.Core.Internal;

namespace Panda.Core.Blocks
{
    public interface IBlock : ICacheKeyed<BlockOffset>
    {
        BlockOffset Offset { get; }
        ReaderWriterLockSlim Lock { get; }
    }
}