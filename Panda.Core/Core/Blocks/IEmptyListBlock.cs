using System;
using JetBrains.Annotations;

namespace Panda.Core.Blocks
{
    /// <summary>
    /// A block that contains a list of empty blocks along with the number of known empty blocks.
    /// </summary>
    /// <remarks>
    ///     <para>This block isn't technically part of the blocks API, because the file system 
    ///     can simply use <see cref="IBlockManager.AllocateDataBlock"/>, <see cref="IBlockManager.AllocateDirectoryBlock"/>, etc. 
    ///     and <see cref="IBlockManager.FreeBlock"/>.</para>
    /// </remarks>
    public interface IEmptyListBlock : IOffsetListBlock
    {
        /// <summary>
        ///  The total number of free blocks recorded on this empty list block and all its continuation blocks.
        ///  </summary><remarks><para>The <see cref="P:Panda.Core.Blocks.IEmptyListBlock.TotalFreeBlockCount" /> on continuation blocks is likely lower than the <see cref="P:Panda.Core.Blocks.IEmptyListBlock.TotalFreeBlockCount" /> on this block, as they only cover blocks "further down the line".</para></remarks>
        int TotalFreeBlockCount { get; }

        /// <summary>
        ///  Removes a number of free block offsets from this list block.
        ///  </summary><param name="count">The number of free block offsets to remove.</param><returns>The removed free block offsets.</returns><exception cref="T:System.ArgumentOutOfRangeException">Not enough free block offsets in this block to satisfy the request.</exception>
        BlockOffset[] Remove(int count);

        /// <summary>
        ///  Add the specified set of free block offsets to this empty block list.
        ///  </summary><param name="freeBlockOffsets">Set of free block offsets to be added.</param><exception cref="T:System.ArgumentNullException"><paramref name="freeBlockOffsets" /> is null</exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="freeBlockOffsets" /> does not fit entirely into this block.</exception>
        void Append(BlockOffset[] freeBlockOffsets);
    }
}