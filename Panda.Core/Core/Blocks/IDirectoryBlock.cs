namespace Panda.Core.Blocks
{
    public interface IDirectoryBlock : IDirectoryContinuationBlock
    {
        /// <summary>
        ///  Total size of data in this directory in bytes.
        ///  </summary><remarks>Will be less than actual bytes occupied in the file system.</remarks>
        long TotalSize { get; }
    }
}