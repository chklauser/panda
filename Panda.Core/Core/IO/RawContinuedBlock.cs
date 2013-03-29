using Panda.Core.Blocks;

namespace Panda.Core.IO
{
    public class RawContinuedBlock : RawBlock, IContinuationBlock
    {
        public RawContinuedBlock(IRawPersistenceSpace space, BlockOffset offset, uint blockSize) : base(space, offset, blockSize)
        {
        }

        protected unsafe BlockOffset* ContinuationBlockSlot
        {
            get
            {
                return (BlockOffset*)(((byte*)ThisPointer) + BlockSize - sizeof(BlockOffset));
            }
        }

        public unsafe BlockOffset? ContinuationBlockOffset
        {
            get
            {
                var bo = *ContinuationBlockSlot;
                if (bo.Offset == 0)
                    return null;
                else
                    return bo;
            }
            set
            {
                if (value == null)
                {
                    *ContinuationBlockSlot = (BlockOffset)0;
                }
                else
                {
                    *ContinuationBlockSlot = value.Value;
                }
            }
        }
    }
}