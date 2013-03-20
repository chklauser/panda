using System.Collections;
using System.Collections.Generic;

namespace Panda.Core
{
    public abstract class FileBlock : Block, IReadOnlyList<int>
    {
        /// <summary>
        /// The actual size of the file in bytes.
        /// </summary>
        /// <remarks>The size a file occupies in the virtual disk can be larger (rounded up to multiples of block size etc.).</remarks>
        public abstract long Size { get; }

        public abstract IEnumerator<int> GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// The number of data block offsets in this file block.
        /// </summary>
        public abstract int Count { get; }

        /// <summary>
        /// Access to the n-th data block offset in this block. Does not automatically include continuation blocks.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public abstract int this[int index] { get; }
    }
}