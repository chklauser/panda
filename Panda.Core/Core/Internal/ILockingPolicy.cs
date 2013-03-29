using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Panda.Core.Blocks;

namespace Panda.Core
{
    /// <summary>
    /// A locking policy for situations where multiple blocks must be locked at once.
    /// </summary>
    internal interface ILockingPolicy
    {
        /// <summary>
        /// Blocks until locks on all supplied blocks have been acquired.
        /// </summary>
        /// <param name="readLocked">Set of blocks to be read-locked. Can be null.</param>
        /// <param name="writeLocked">Set of blocks to be write-locked. Can be null.</param>
        /// <returns>A handle that, when disposed (<see cref="IDisposable.Dispose"/>) will exit the locks.</returns>
        /// <remarks>
        /// <para>If a block is supplied both for read-locking and for write-locking, it is going to be write-locked.</para>
        /// <para>Whether the order of blocks matters, depends on the concrete implementation of the locking policy.</para>
        /// <code>using(yourLockingPolicy.Enter(readLocked: new[]{rBlock1, rBlock2}, writeLocked: new[]{ wBlock1 }))
        /// {
        ///     // ... your code where you have a read lock on rBlock1 and rBlock2 and a write lock on wBlock1.
        /// }
        /// // Locks are removed once you have left this region</code>
        /// </remarks>
        [NotNull]
        IDisposable Enter([CanBeNull] IBlock[] readLocked = null, [CanBeNull] IBlock[] writeLocked = null);
    }
}