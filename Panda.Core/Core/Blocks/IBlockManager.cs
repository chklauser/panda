namespace Panda.Core.Blocks
{
    public interface IBlockManager
    {
        #region Block allocation

        IDirectoryBlock AllocateDirectoryBlock();
        IDirectoryContinuationBlock AllocateDirectoryContinuationBlock();
        IFileBlock AllocateFileBlock();
        IFileContinuationBlock AllocateFileContinuationBlock();

        void FreeBlock(int blockOffset);

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
        IDirectoryBlock GetDirectoryBlock(int blockOffset);

        /// <summary>
        /// Returns a representation of the block at the specified offset, interpreted as an <see cref="IDirectoryContinuationBlock"/>.
        /// </summary>
        /// <param name="blockOffset">The offset at which the <see cref="IDirectoryContinuationBlock "/> is located.</param>
        /// <returns>A representation of the block at the specified <paramref name="blockOffset"/>.</returns>
        /// <remarks>Implementations of <see cref="IBlockManager"/> may or may not check whether the block located at the specified 
        /// <paramref name="blockOffset"/> actually is a <see cref="IDirectoryContinuationBlock"/>. When the block isn't of the correct type, 
        /// the behaviour of this method and the returned <see cref="IDirectoryContinuationBlock"/> is unspecified.</remarks>
        IDirectoryContinuationBlock GetDirectoryContinuationBlock(int blockOffset);

        /// <summary>
        /// Returns a representation of the block at the specified offset, interpreted as an <see cref="IFileBlock"/>.
        /// </summary>
        /// <param name="blockOffset">The offset at which the <see cref="IFileBlock "/> is located.</param>
        /// <returns>A representation of the block at the specified <paramref name="blockOffset"/>.</returns>
        /// <remarks>Implementations of <see cref="IBlockManager"/> may or may not check whether the block located at the specified 
        /// <paramref name="blockOffset"/> actually is a <see cref="IFileBlock"/>. When the block isn't of the correct type, 
        /// the behaviour of this method and the returned <see cref="IFileBlock"/> is unspecified.</remarks>
        IFileBlock GetFileBlock(int blockOffset);

        /// <summary>
        /// Returns a representation of the block at the specified offset, interpreted as an <see cref="IFileContinuationBlock"/>.
        /// </summary>
        /// <param name="blockOffset">The offset at which the <see cref="IFileContinuationBlock "/> is located.</param>
        /// <returns>A representation of the block at the specified <paramref name="blockOffset"/>.</returns>
        /// <remarks>Implementations of <see cref="IBlockManager"/> may or may not check whether the block located at the specified 
        /// <paramref name="blockOffset"/> actually is a <see cref="IFileContinuationBlock"/>. When the block isn't of the correct type, 
        /// the behaviour of this method and the returned <see cref="IFileContinuationBlock"/> is unspecified.</remarks>
        IFileContinuationBlock GetFileContinuationBlock(int blockOffset);

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
        int RootDirectoryBlockOffset { get; }

        #endregion

    }
}