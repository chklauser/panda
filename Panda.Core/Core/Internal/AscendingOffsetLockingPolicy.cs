using System;
using JetBrains.Annotations;
using Panda.Core.Blocks;

namespace Panda.Core
{
    class AscendingOffsetLockingPolicy : ILockingPolicy
    {
        private static readonly IBlock[] Empty = new IBlock[0];

        public IDisposable Enter(IBlock[] readLocked = null, IBlock[] writeLocked = null)
        {
            if (readLocked == null)
                readLocked = Empty;
            if (writeLocked == null)
                writeLocked = Empty;

            var order = new Tuple<bool, IBlock>[readLocked.Length + writeLocked.Length];
            var i = 0;
            foreach (var block in readLocked)
                order[i++] = Tuple.Create(false, block);
            foreach (var block in writeLocked)
                order[i++] = Tuple.Create(true, block);

            Array.Sort(order, _blockLockingOrder);

            BlockOffset lastOffset = default(BlockOffset);

            try
            {
                for (i = 0; i < order.Length; i++)
                {
                    var tuple = order[i];
                    var block = tuple.Item2;

                    // Check whether we have seen this block offset already 
                    // (since the array is sorted all instances of the same offset will sit next to each other)
                    // We silently ignore duplicates. The fact that write-lock-requests are ordered 
                    // before read-lock-requests ensures that we act on the write-lock-request and ignore the
                    // read-lock-request.
                    if (lastOffset == block.Offset)
                    {
                        // set duplicate to null, so that we don't have to check for this condition later
                        order[i] = null;
                        continue;
                    }
                    lastOffset = block.Offset;

                    if (tuple.Item1)
                        block.Lock.EnterWriteLock();
                    else
                        block.Lock.EnterReadLock();
                }
            }
            finally
            {
                // Something went wrong while locking, unlock everything we have locked so far.
                // But we can't handle the exception here, just revert the actions that we have taken so far.
                _unlockInReverse(i, order);
            }

            // If we read this point, we know that we have successfully locked all blocks
            return new UnlockHandle(order);
        }

        /// <summary>
        /// Iterates over an array of blocks in reverse and unlocks them.
        /// </summary>
        /// <param name="i">Index of first (right-most) element to unlock.</param>
        /// <param name="order"></param>
        private static void _unlockInReverse(int i, Tuple<bool, IBlock>[] order)
        {
            for (; i >= 0; i--)
            {
                var tuple = order[i];
                if (tuple == null)
                    continue;

                var block = tuple.Item2;
                if (tuple.Item1)
                {
                    if (block.Lock.IsReadLockHeld)
                        block.Lock.ExitReadLock();
                }
                else
                {
                    if (block.Lock.IsWriteLockHeld)
                        block.Lock.ExitWriteLock();
                }
            }
        }

        class UnlockHandle : IDisposable
        {
            [NotNull]
            private readonly Tuple<bool, IBlock>[] _order;

            public UnlockHandle([NotNull] Tuple<bool, IBlock>[] order)
            {
                _order = order;
            }

            public void Dispose()
            {
                _unlockInReverse(_order.Length - 1, _order);
            }
        }

        /// <summary>
        /// Decides how block <paramref name="x"/> relates to <paramref name="y"/> in the locking ordering.
        /// </summary>
        /// <param name="x">A tuple consisting of a boolean flag that indicates whether the block needs to be write-locked (false means read-locked) and a block.</param>
        /// <param name="y">Another tuple consisting of a boolean flag that indicates whether the block needs to be write-locked (false means read-locked) and a block.</param>
        /// <returns>less than 0 if <paramref name="x"/> comes before <paramref name="y"/>; 
        /// 0 if <paramref name="x"/> and <paramref name="y"/> are equal; 
        /// greater than 0 if <paramref name="x"/> is greater than <paramref name="y"/>.</returns>
        /// <remarks>Orders by block offset in ascending order. If a block appears more than once, write lock requests are ordered before read lock requests.</remarks>
        private int _blockLockingOrder([NotNull] Tuple<bool, IBlock> x, [NotNull] Tuple<bool, IBlock> y)
        {
            // It is safe to extract the offset here, as we are just using it to order blocks.
            // Note that we have to cast to long instead of uint, to capture negative numbers
            long cmp;
            checked
            {
                cmp = (long)x.Item2.Offset - (long)y.Item2.Offset;
            }
            if (cmp == 0)
            {
                if (x.Item1 && !x.Item1)
                    return 1;
                if (!x.Item1 && x.Item1)
                    return -1;
            }

            return Math.Sign(cmp);
        }
    }
}