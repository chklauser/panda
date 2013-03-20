using Panda.Core.Blocks;

namespace Panda.Core
{
    public interface IBlockManager
    {
        #region Block allocation

        DirectoryBlock AllocateDirectoryBlock();
        DirectoryContinuationBlock AllocateDirectoryContinuationBlock();
        FileBlock AllocateFileBlock();
        FileContinuationBlock AllocateFileContinuationBlock();

        void FreeBlock(int blockOffset);

        #endregion

        #region Block retrieval

        DirectoryBlock GetDirectoryBlock(int blockOffset);
        DirectoryContinuationBlock GetDirectoryContinuationBlock(int blockOffset);
        FileBlock GetFileBlock(int blockOffset);
        FileContinuationBlock GetFileContinuationBlock(int blockOffset);

        #endregion

    }
}