using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Panda.Core.Blocks;

namespace Panda.Core.IO
{
    public class RawOffsetListBlock : RawContinuedBlock, IFileContinuationBlock
    {
        public RawOffsetListBlock([NotNull] RawBlockManager manager, BlockOffset offset, uint size)
            : base(manager, offset, size)
        {
        }

        unsafe uint _getUIntAt(int index)
        {
            var ptr = (uint*) ThisPointer;
            return ptr[index];
        }

        public IEnumerator<BlockOffset> GetEnumerator()
        {
            // Iterate over offsets until we reach a null entry or the end of the block.
            for (var i = 0; i < ListCapacity; i++)
            {
                // The first uint is meta information (total offset count/file size)
                var value = _getUIntAt(i + MetaDataPrefixUInt32Count);
                if(value == 0)
                    yield break;
                else
                    yield return (BlockOffset) value;
            }
        }

        /// <summary>
        /// The number of UInt32 fields at the beginning of the offset block.
        /// Contains information about the block (total offset count, file size, etc.)
        /// </summary>
        protected virtual int MetaDataPrefixUInt32Count
        {
            get { return 0; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            // we don't actually have a better way to compute the count than to parse the block, offset by offset
            // just counting would be a bit more efficient than asking an enumerator, but Count is not
            // a property that will be used often.
// ReSharper disable InvokeAsExtensionMethod
            get { return Enumerable.Count(this); }
// ReSharper restore InvokeAsExtensionMethod
        }

        public unsafe int ListCapacity
        {
            get
            {
                // The number of offsets that fit into the block minus total offset count and continuation offset
                return (int) (BlockSize/sizeof (BlockOffset) - MetaDataPrefixUInt32Count - sizeof(BlockOffset));
            }
        }

        public unsafe void ReplaceOffsets(BlockOffset[] offsets)
        {
            if (offsets == null)
                throw new ArgumentNullException("offsets");
            
            if (offsets.Length > ListCapacity)
            {
                throw new ArgumentOutOfRangeException("offsets",offsets.Length,"Not all block offsets fit into this block.");
            }

            // Replace all offsets in the block with the supplied offsets, 
            // padding with 0 if the array is too short.
            // 

            var ptr = ((uint*) ThisPointer)+MetaDataPrefixUInt32Count;
            
            for (var i = 0; i < ListCapacity; i++)
            {
                if (i < offsets.Length)
                {
                    ptr[i] = offsets[i].Offset;
                }
                else
                {
                    ptr[i] = 0u;
                }
            }

            OnBlockChanged();
        }
    }
}
