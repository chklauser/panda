using JetBrains.Annotations;

namespace Panda
{
    public static class VirtualFileSystem
    {
        /// <summary>
        /// The path separator used by the Panda virtual file system.
        /// </summary>
        [PublicAPI]
        public const char SeparatorChar = '/';
    }
}