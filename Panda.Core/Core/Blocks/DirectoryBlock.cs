namespace Panda.Core.Blocks
{
    public abstract class DirectoryBlock : DirectoryEntryListBlock
    {
        /// <summary>
        /// Total size of data in this directory in bytes.
        /// </summary>
        /// <remarks>Will be less than actual bytes occupied in the file system.</remarks>
        public abstract long TotalSize { get; }
    }
}