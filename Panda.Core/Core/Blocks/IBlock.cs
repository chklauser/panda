using System.Threading;

namespace Panda.Core.Blocks
{
    public interface IBlock
    {
        BlockOffset Offset { get; }
        ReaderWriterLockSlim Lock { get; }
    }
}