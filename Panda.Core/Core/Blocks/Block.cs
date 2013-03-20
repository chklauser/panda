using System.Threading;

namespace Panda.Core.Blocks
{
    public abstract class Block
    {
        public abstract int Offset { get; }
        public abstract ReaderWriterLockSlim Lock { get; }
        public abstract long? ContinuationBlock { get; }
    }
}