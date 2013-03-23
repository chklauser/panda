using System;

namespace Panda.Core.Blocks
{
    public interface IBlockManager
    {
        #region Block allocation

        /// <summary>
        /// Allocates a block and initializes it as an empty directory block.
        /// </summary>
        /// <returns>A fresh empty directory block.</returns>
        /// <exception cref="OutofDiskSpaceException">The virtual disk has no empty blocks left.</exception>
        /// <remarks>
        ///     <para>The returned block is not registered anywhere. If you don't add it to the file system and forget its block offset, the block can never be reclaimed.</para>
        /// </remarks>
        IDirectoryBlock AllocateDirectoryBlock();

        /// <summary>
        /// Allocates a block and initializes it as an empty directory continuation block.
        /// </summary>
        /// <returns>A fresh empty directory block.</returns>
        /// <exception cref="OutofDiskSpaceException">The virtual disk has no empty blocks left.</exception>
        /// <remarks>
        ///     <para>The returned block is not registered anywhere. If you don't add it to the file system and forget its block offset, the block can never be reclaimed.</para>
        /// </remarks>
        IDirectoryContinuationBlock AllocateDirectoryContinuationBlock();

        /// <summary>
        /// Allocates a block and initializes it as an empty file block.
        /// </summary>
        /// <returns>A fresh empty file block.</returns>
        /// <exception cref="OutofDiskSpaceException">The virtual disk has no empty blocks left.</exception>
        /// <remarks>
        ///     <para>The returned block is not registered anywhere. If you don't add it to the file system and forget its block offset, the block can never be reclaimed.</para>
        /// </remarks>
        IFileBlock AllocateFileBlock();

        /// <summary>
        /// Allocates a block and initializes it as an empty file continuation block.
        /// </summary>
        /// <returns> A fresh empty file continuation block.</returns>
        /// <exception cref="OutofDiskSpaceException">The virtual disk has no empty blocks left.</exception>
        /// <remarks>
        ///     <para>The returned block is not registered anywhere. If you don't add it to the file system and forget its block offset, the block can never be reclaimed.</para>
        /// </remarks>
        IFileContinuationBlock AllocateFileContinuationBlock();

        /// <summary>
        /// Allocates a raw block to be used as a data block. Initial contents are not defined.
        /// </summary>
        /// <returns>A fresh, uninitialized data block.</returns>
        /// <exception cref="OutofDiskSpaceException">The virtual disk has no empty blocks left.</exception>
        /// <remarks>
        ///     <para>The returned block is not registered anywhere. If you don't add it to the file system and forget its block offset, the block can never be reclaimed.</para>
        /// </remarks>
        BlockOffset AllocateDataBlock();

        /// <summary>
        /// Marks the specified block as free, making it available for allocation. Will not break up existing references.
        /// </summary>
        /// <param name="blockOffset">The block to be freed.</param>
        /// <remarks>
        ///     <para>The file system is responsible for making sure that all references to the block in question have been removed before that block is freed.</para>
        ///     <para>Freed blocks may throw <see cref="BlockDeallocatedException"/>s when interacted with, but there is no guarantee that such behaviour will be detected.</para>
        /// </remarks>
        void FreeBlock(BlockOffset blockOffset);

        #endregion

        #region Block retrieval

        /// <summary>
        /// Returns a representation of the block at the specified offset, interpreted as an <see cref="IDirectoryBlock"/>.
        /// </summary>
        /// <param name="blockOffset">The offset at which the <see cref="IDirectoryBlock "/> is located.</param>
        /// <returns>A representation of the block at the specified <paramref name="blockOffset"/>.</returns>
        /// <remarks>Implementations of <see cref="IBlockManager"/> may or may not check whether the block located at the specified 
        /// <paramref name="blockOffset"/> actually is a <see cref="IDirectoryBlock"/>. When the block isn't of the correct type, 
        /// the behaviour of this method and the returned <see cref="IDirectoryBlock"/> is unspecified.</remarks>
        IDirectoryBlock GetDirectoryBlock(BlockOffset blockOffset);

        /// <summary>
        /// Returns a representation of the block at the specified offset, interpreted as an <see cref="IDirectoryContinuationBlock"/>.
        /// </summary>
        /// <param name="blockOffset">The offset at which the <see cref="IDirectoryContinuationBlock "/> is located.</param>
        /// <returns>A representation of the block at the specified <paramref name="blockOffset"/>.</returns>
        /// <remarks>Implementations of <see cref="IBlockManager"/> may or may not check whether the block located at the specified 
        /// <paramref name="blockOffset"/> actually is a <see cref="IDirectoryContinuationBlock"/>. When the block isn't of the correct type, 
        /// the behaviour of this method and the returned <see cref="IDirectoryContinuationBlock"/> is unspecified.</remarks>
        IDirectoryContinuationBlock GetDirectoryContinuationBlock(BlockOffset blockOffset);

        /// <summary>
        /// Returns a representation of the block at the specified offset, interpreted as an <see cref="IFileBlock"/>.
        /// </summary>
        /// <param name="blockOffset">The offset at which the <see cref="IFileBlock "/> is located.</param>
        /// <returns>A representation of the block at the specified <paramref name="blockOffset"/>.</returns>
        /// <remarks>Implementations of <see cref="IBlockManager"/> may or may not check whether the block located at the specified 
        /// <paramref name="blockOffset"/> actually is a <see cref="IFileBlock"/>. When the block isn't of the correct type, 
        /// the behaviour of this method and the returned <see cref="IFileBlock"/> is unspecified.</remarks>
        IFileBlock GetFileBlock(BlockOffset blockOffset);

        /// <summary>
        /// Returns a representation of the block at the specified offset, interpreted as an <see cref="IFileContinuationBlock"/>.
        /// </summary>
        /// <param name="blockOffset">The offset at which the <see cref="IFileContinuationBlock "/> is located.</param>
        /// <returns>A representation of the block at the specified <paramref name="blockOffset"/>.</returns>
        /// <remarks>Implementations of <see cref="IBlockManager"/> may or may not check whether the block located at the specified 
        /// <paramref name="blockOffset"/> actually is a <see cref="IFileContinuationBlock"/>. When the block isn't of the correct type, 
        /// the behaviour of this method and the returned <see cref="IFileContinuationBlock"/> is unspecified.</remarks>
        IFileContinuationBlock GetFileContinuationBlock(BlockOffset blockOffset);

        /// <summary>
        /// Overwrites the specified data block. 
        /// </summary>
        /// <param name="blockOffset">The offset of the data block to write to.</param>
        /// <param name="data">The data to overwrite the data block with. If shorter than <see cref="DataBlockSize"/> will be padded with zeroes.</param>
        /// <remarks><para>Implementations may or may not guard against writing to non-allocated or non-data blocks.</para></remarks>
        void WriteDataBlock(BlockOffset blockOffset, byte[] data);

        /// <summary>
        /// Reads the specified block into the supplied array.
        /// </summary>
        /// <param name="blockOffset">The offset of the data block to read from.</param>
        /// <param name="destination">The array to copy the data into.</param>
        /// /// <param name="destinationIndex">The index in the destination array at which to start writing. (inclusive)</param>
        /// <param name="blockIndex">The offset in the data block at which to start reading.</param>
        /// <param name="count">The number of bytes to read at most.</param>
        /// <exception cref="ArgumentOutOfRangeException">Negative array index or count; attempt to read past end of block;</exception>
        void ReadDataBlock(BlockOffset blockOffset, byte[] destination, int destinationIndex = 0, int blockIndex = 0, int? count = null);

        #endregion

        #region Meta data block access

        // Not all meta data block elements are represented on this API level
        // for instance the block size and empty block list offset are only relevant
        // to the *implementation* of the block API (Panda.Core.Blocks.*)

        /// <summary>
        /// Number of blocks on the virtual disk. Also counts the "meta" block, even though it is not represented as an IBlock in the system.
        /// </summary>
        int TotalBlockCount { get; }

        /// <summary>
        /// Offset of the <see cref="IDirectoryBlock"/> of the root directory.
        /// </summary>
        BlockOffset RootDirectoryBlockOffset { get; }

        /// <summary>
        /// Number of bytes that fit into a data block.
        /// </summary>
        int DataBlockSize { get; }

        #endregion

    }
}