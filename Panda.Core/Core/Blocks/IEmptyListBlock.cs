using System;
using JetBrains.Annotations;

namespace Panda.Core.Blocks
{
    public interface IEmptyListBlock : IOffsetListBlock, IContinuationBlock
    {
        /// <summary>
        ///  The total number of free blocks recorded on this empty list block and all its continuation blocks.
        ///  </summary><remarks><para>The <see cref="P:Panda.Core.Blocks.IEmptyListBlock.TotalFreeBlockCount" /> on continuation blocks is likely lower than the <see cref="P:Panda.Core.Blocks.IEmptyListBlock.TotalFreeBlockCount" /> on this block, as they only cover blocks "further down the line".</para></remarks>
        int TotalFreeBlockCount { get; }

        /// <summary>
        ///  Removes a number of free block offsets from this list block.
        ///  </summary><param name="count">The number of free block offsets to remove.</param><returns>The removed free block offsets.</returns><exception cref="T:System.ArgumentOutOfRangeException">Not enough free block offsets in this block to satisfy the request.</exception>
        int[] Remove(int count);

        /// <summary>
        ///  Add the specified set of free block offsets to this empty block list.
        ///  </summary><param name="freeBlockOffsets">Set of free block offsets to be added.</param><exception cref="T:System.ArgumentNullException"><paramref name="freeBlockOffsets" /> is null</exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="freeBlockOffsets" /> does not fit entirely into this block.</exception>
        void Append(int[] freeBlockOffsets);

        /// <summary>
        ///  Indicates how many empty block offsets this empty block list can contain.
        ///  </summary><remarks><see cref="P:Panda.Core.Blocks.IEmptyListBlock.ListCapacity" /> minus <see cref="P:System.Collections.Generic.IReadOnlyCollection`1.Count" /> 
        ///  equals number of empty block offsets you can add to this block until 
        ///  a continuation block needs to be created.</remarks>
        int ListCapacity { get; }
    }
}