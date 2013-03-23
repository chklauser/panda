using System;

namespace Panda.Core.IO
{
    /// <summary>
    /// A persistence space provides low-level access to a storage space that can be or is persisted.
    /// </summary>
    /// <remarks>This is an "abstract" interface in the sense that it isn't useful by itself. See <see cref="IRawPersistenceSpace"/> for a concrete persistence space interface.</remarks>
    public interface IPersistenceSpace : IDisposable
    {
        /// <summary>
        /// Total capacity of the persistence space in number of bytes.
        /// </summary>
        long Capacity { get; }

        /// <summary>
        /// Indicates whether the space can be resized.
        /// </summary>
        bool CanResize { get; }

        /// <summary>
        /// Resizes the space, maintaining content that isn't in a region affected by the resizing.
        /// </summary>
        /// <param name="newSize">The desired size (capacity)</param>
        void Resize(long newSize);

        /// <summary>
        /// Forces the persistence space to commit pending (buffered) changes.
        /// </summary>
        /// <remarks>In the interest of efficiency, persistence spaces may buffer changes before commiting them to persistent storage.</remarks>
        void Flush();
    }
}