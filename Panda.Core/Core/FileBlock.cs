using System.Collections;
using System.Collections.Generic;

namespace Panda.Core
{
    public abstract class FileBlock : OffsetListBlock
    {
        /// <summary>
        /// The actual size of the file in bytes.
        /// </summary>
        /// <remarks>The size a file occupies in the virtual disk can be larger (rounded up to multiples of block size etc.).</remarks>
        public abstract long Size { get; }
    }
}