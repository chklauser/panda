using System.Collections.Generic;

namespace Panda.Core.Blocks
{
    /// <summary>
    /// A block that records journal entries (<see cref="JournalEntry"/>).
    /// </summary>
    public interface IJournalBlock : IContinuationBlock, IEnumerable<JournalEntry>
    {
        /// <summary>
        /// Attempts to append a new journal entry. Fails if there is not enough space left.
        /// </summary>
        /// <param name="entry">The journal entry to add.</param>
        /// <returns>True if the entry was added successfully; false if there was not enough space left.</returns>
        /// <remarks>Only returns false if there was not enough space to add the entry. 
        /// In all other error conditions, an exception is thrown.</remarks>
        bool TryAppendEntry(JournalEntry entry);
    }
}