using System.Threading;

namespace Panda.Core.Blocks
{
    public interface IBlock
    {
        int Offset { get; }
        ReaderWriterLockSlim Lock { get; }
    }
}