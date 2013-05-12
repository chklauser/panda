using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using Panda.Core.Blocks;

namespace Panda
{
    /// <summary>
    /// A disk that can synchronize its contents with other disks. Assumes block-based underlying organisation.
    /// </summary>
    public interface ISynchronizingDisk
    {
        /// <summary>
        /// Retrieves the sequence of changes made to the disk between a specified point in time and now.
        /// </summary>
        /// <param name="lastSynchronization">Date of the latest change not to include.</param>
        /// <returns>A sequence of changes in reverse-chronological order (newest to oldest)</returns>
        [NotNull]
        IEnumerable<JournalEntry> GetJournalEntriesSince(DateTime lastSynchronization);

        /// <summary>
        /// Incorporate an updated block into the disk. This is not treated as a change and will thus
        /// not generate a journal entry.
        /// </summary>
        /// <param name="blockOffset">Offset of the block to update.</param>
        /// <param name="data">The data to write to the block.</param>
        void ReceiveChanges(BlockOffset blockOffset, [NotNull] byte[] data);

        /// <summary>
        /// Copies any block on the disk into the designated stream.
        /// </summary>
        /// <param name="blockOffset">The offset of the block to read.</param>
        /// <param name="destination">Buffer to copy the block's contents to.</param>
        /// <param name="index">Index into the destination buffer at which to start writing.</param>
        /// <remarks>Always reads the entire block.</remarks>
        void DirectRead(BlockOffset blockOffset, [NotNull] byte[] destination, int index);

        /// <summary>
        /// Associates the (client) disk with a server disk or removes such an association.
        /// </summary>
        /// <param name="serverDiskName">The name the disk has on the server. Or null to remove existing associations.</param>
        /// <exception cref="ArgumentException">Server disk name is too long to fit into the meta data block of the disk</exception>
        void Associate([CanBeNull] string serverDiskName);

        /// <summary>
        /// Informs the disk that synchronization was successful.
        /// </summary>
        void NotifySynchronized();

        int BlockSize { get; }
    }
}