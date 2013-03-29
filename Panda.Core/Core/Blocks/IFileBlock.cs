namespace Panda.Core.Blocks
{
    public interface IFileBlock : IFileContinuationBlock
    {
        /// <summary>
        ///  The actual size of the file in bytes.
        ///  </summary><remarks>The size a file occupies in the virtual disk can be larger (rounded up to multiples of block size etc.).</remarks>
        long Size { get; set; }
    }
}