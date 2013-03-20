using System.Collections;
using System.Collections.Generic;

namespace Panda.Core
{
    public abstract class DirectoryBlock : Block, IReadOnlyList<DirectoryEntry>
    {
        /// <summary>
        /// Total size of data in this directory in bytes.
        /// </summary>
        /// <remarks>Will be less than actual bytes occupied in the file system.</remarks>
        public abstract long TotalSize { get; }

        public abstract IEnumerator<DirectoryEntry> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public abstract void DeleteEntry(DirectoryEntry entry);
        public abstract void AddEntry(DirectoryEntry entry);

        public abstract int Count { get; }
        public abstract DirectoryEntry this[int index] { get; }
    }
}