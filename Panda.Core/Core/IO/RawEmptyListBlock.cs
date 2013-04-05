using System;
using System.Linq;
using JetBrains.Annotations;
using Panda.Core.Blocks;

namespace Panda.Core.IO
{
    class RawEmptyListBlock : RawOffsetListBlock, IEmptyListBlock
    {
        public RawEmptyListBlock([NotNull] IRawPersistenceSpace space, BlockOffset offset, uint size) : base(space, offset, size)
        {
        }

        public unsafe int TotalFreeBlockCount
        {
            get
            {
                return *TotalFreeBlockCountSlot;
            }
            set
            {
                if(value < 0)
                    throw new ArgumentOutOfRangeException("value",value,"TotalFreeBlockCount cannot be negative.");
                *TotalFreeBlockCountSlot = value;
            }
        }

        protected unsafe int* TotalFreeBlockCountSlot
        {
            get
            {
                return ((int*) ThisPointer) + base.MetaDataPrefixUInt32Count;
            }
        }

        public BlockOffset[] Remove(int count)
        {
            var offsets = this.ToArray();
            if (offsets.Length < count)
            {
                throw new ArgumentOutOfRangeException("count",count,"Not enough offsets in the block to satisfy the remove request.");
            }

            // Move offsets to result, setting them to 0 in the offset-arry
            var result = new BlockOffset[count];
            for (var i = 0; i < result.Length; i++)
            {
                var revIdx = offsets.Length - 1 - i;
                result[i] = offsets[revIdx];
                offsets[revIdx] = (BlockOffset) 0;
            }

            // Write back the modified offsets
            ReplaceOffsets(offsets);

            // Also update the free block count
            TotalFreeBlockCount -= count;

            return result;
        }

        public void Append(BlockOffset[] freeBlockOffsets)
        {
            if (freeBlockOffsets == null)
                throw new ArgumentNullException("freeBlockOffsets");
            
            // This is a very simplistic implementation of append:
            //  reading the entire blocklist, appending the new offsets in memory and
            //  then writing the entire list back.

            // A more sophisticated implementation would just write the modified entries.
            var offsets = this.ToList();
            offsets.AddRange(freeBlockOffsets);
            if(offsets.Count > ListCapacity)
                throw new ArgumentOutOfRangeException("freeBlockOffsets",freeBlockOffsets.Length,"Not all offsets fit into this block.");

            ReplaceOffsets(offsets.ToArray());
            TotalFreeBlockCount += freeBlockOffsets.Length;
        }

        protected override int MetaDataPrefixUInt32Count
        {
            get { return base.MetaDataPrefixUInt32Count + 1; }
        }
    }
}