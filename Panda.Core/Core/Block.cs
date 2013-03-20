using System;
using System.Threading;
using JetBrains.Annotations;

namespace Panda.Core
{
    public abstract class Block
    {
        public abstract int Offset { get; }
        public abstract ReaderWriterLockSlim Lock { get; }
        public abstract long? ContinuationBlock { get; }
    }
}