using System.Collections;
using System.Collections.Generic;

namespace Panda.Core
{
    public abstract class OffsetListBlock : Block, IReadOnlyList<int>
    {
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