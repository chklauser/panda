using System;
using System.IO;
using System.Security;
using JetBrains.Annotations;

namespace Panda
{
    [PublicAPI]
    public abstract class VirtualDisk
    {
        #region Virtual disk management API

        /// <summary>
        /// Opens an existing virtual disk.
        /// </summary>
        /// <param name="path">Path to a Panda virtual disk file in the local file system.</param>
        /// <returns>A <see cref="VirtualDisk"/> object that can be used to interact with the virtual file system located on the disk.</returns>
        /// <exception cref="ArgumentNullException">path is null</exception>
        /// <exception cref="ArgumentException">path is empty</exception>
        /// <exception cref="SecurityException">Caller does not have enough permissions to access the file.</exception>
        [PublicAPI]
        [NotNull]
        public static VirtualDisk OpenExisting([NotNull] string path)
        {
            throw new NotImplementedException("VirtualDisk::OpenExisting");
        }

        /// <summary>
        /// Creates a new virtual disk.
        /// </summary>
        /// <param name="path">Path where the Panda virtual disk file should be created in the local file system.</param>
        /// <returns>A <see cref="VirtualDisk"/> object that can be used to interact with the virtual file system located on the disk.</returns>
        /// <exception cref="ArgumentNullException">path is null</exception>
        /// <exception cref="ArgumentException">path is empty</exception>
        /// <exception cref="SecurityException">Caller does not have enough permissions to create the file.</exception>
        /// <exception cref="InvalidOperationException">The file already exists.</exception>
        [PublicAPI]
        [NotNull]
        public static VirtualDisk CreateNew([NotNull] string path)
        {
            throw new NotImplementedException("VirtualDisk::Create");
        }

        /// <summary>
        /// Deletes an existing virtual disk.
        /// </summary>
        /// <remarks>If no such file exists, the call is silently ignored.</remarks>
        /// <param name="path">Path to a Panda virtual disk file in the local file system.</param>
        /// <exception cref="ArgumentNullException">path is null</exception>
        /// <exception cref="ArgumentException">path is empty</exception>
        /// <exception cref="SecurityException">Caller does not have enough permissions to create the file.</exception>
        [PublicAPI]
        public static void DeleteExisting([NotNull] string path)
        {
            File.Delete(path);
        }

        #endregion

        #region Virtual disk space usage API

        // May add methods for extending (possibly shrinking) a virtual disk later

        /// <summary>
        /// Maximum disk capacity in bytes. The disk will not grow beyond this size.
        /// </summary>
        [PublicAPI]
        public abstract long Capacity { get; }

        /// <summary>
        /// Number of bytes currently occupied by the file system. Includes enclosed free space.
        /// </summary>
        public abstract long CurrentSize { get; }

        /// <summary>
        /// Number of bytes currently occupied by directories, files and data.
        /// </summary>
        [PublicAPI]
        public long BytesOccupied
        {
            get { return CurrentSize - FreeSpace; }
        }

        /// <summary>
        /// Number of bytes available for allocation within the file system.
        /// </summary>
        public abstract long FreeSpace { get; }

        /// <summary>
        /// Number of bytes available for allocation on the disk.
        /// </summary>
        [PublicAPI]
        public long BytesRemaining
        {
            get { return Capacity - BytesOccupied; }
        }

        #endregion


    }
}