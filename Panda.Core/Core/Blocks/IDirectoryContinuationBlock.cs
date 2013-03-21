using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Panda.Core.Blocks
{
    public interface IDirectoryContinuationBlock : IBlock, IReadOnlyCollection<DirectoryEntry>, IContinuationBlock
    {
        void DeleteEntry(DirectoryEntry entry);

        /// <summary>
        /// Tries to add the entry to the directory block. Failure indicates that there is not enough room for this entry.
        /// </summary>
        /// <param name="entry">The entry to add.</param>
        /// <returns>true if the entry was added successfully; false if there is not enough room for this entry.</returns>
        bool TryAddEntry([NotNull] DirectoryEntry entry);
    }
}