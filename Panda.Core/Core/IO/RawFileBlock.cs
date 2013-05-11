using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Panda.Core.Blocks;

namespace Panda.Core.IO
{
    internal class RawFileBlock : RawOffsetListBlock, IFileBlock
    {
        public RawFileBlock([NotNull] RawBlockManager manager, BlockOffset offset, uint size) : base(manager, offset, size)
        {
        }

        protected unsafe long* SizeSlot
        {
            get { return (long*) ThisPointer + base.MetaDataPrefixUInt32Count; }
        }

        public unsafe long Size
        {
            get { return *SizeSlot; }
            set
            {
                if(value < 0)
                    throw new ArgumentOutOfRangeException("value",value,"File size cannot be negative.");

                *SizeSlot = value;
                OnBlockChanged();
            }
        }

        protected override int MetaDataPrefixUInt32Count
        {
            get { return base.MetaDataPrefixUInt32Count + sizeof(long)/sizeof(UInt32); }
        }
    }
}
