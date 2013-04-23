namespace Panda.Server.Persistence
{
    /// <summary>
    /// Provides exclusive access to lazily loaded disks.
    /// </summary>
    public interface IDiskRepository
    {
        /// <summary>
        /// Lazily opens existing disks and locks them for exclusive access by the user.
        /// </summary>
        /// <param name="diskName">The (file) name of the disk to open.</param>
        /// <returns>An object that represents the exclusive lock on the disk.</returns>
        /// <remarks>
        /// <code>using(var lease = myDiskRepository["someDisk.panda"])
        /// {
        ///     var disk = lease.Disk;
        ///     // .. you have exclusive access to the disk here
        /// }
        /// // DON'T USE DISK HERE, it might have been closed</code>
        /// </remarks>
        DiskLease this[string diskName] { get; }
    }
}