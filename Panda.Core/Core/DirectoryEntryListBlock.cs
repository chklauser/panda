using System.Collections;
using System.Collections.Generic;

namespace Panda.Core
{
    public abstract class DirectoryEntryListBlock : Block, IReadOnlyList<DirectoryEntry>
    {
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