using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Panda.Core.Blocks;

namespace Panda.Core.IO
{
    class RawJournalBlock : RawContinuedBlock, IJournalBlock
    {
        public RawJournalBlock([NotNull] RawBlockManager manager, BlockOffset offset, uint blockSize) 
            : base(manager, offset, blockSize)
        {
        }

        public unsafe uint ListCapacity
        {
            get
            {
                var entrySize = (uint) sizeof (RawJournalEntry);
                return ((BlockSize - (uint)sizeof(BlockOffset)) / entrySize);
            }
        }

        [StructLayout(LayoutKind.Sequential,Pack = 4)]
        struct RawJournalEntry
        {
            public long BinaryDate;
            public BlockOffset BlockOffset;
        }

        [ContractAnnotation("suppressConstruction:false=>true,entry:notnull;" +
            "suppressConstruction:true=>true,entry:null;" +
            "suppressConstruction:false=>false,entry:null;" +
            "suppressConstruction:true=>false,entry:null")]
        unsafe bool _tryReadJournalEntryAt(int index, out JournalEntry entry, bool suppressConstruction = false)
        {
            // bounds check, mandatory as the callers won't check
            if (index >= ListCapacity)
            {
                entry = null;
                return false;
            }

            // read the raw entry
            var ptr = (RawJournalEntry*)ThisPointer;
            var raw = ptr[index];
            // and verify that it is a valid entry.
            if (raw.BinaryDate != 0)
            {
                entry = suppressConstruction 
                    ? null 
                    : new JournalEntry(DateTime.FromBinary(raw.BinaryDate), raw.BlockOffset);
                return true;
            }
            else
            {
                entry = null;
                return false;
            }
        }

        unsafe void _writeJournalEntryAt(int index, JournalEntry entry)
        {
            var raw = new RawJournalEntry
                {
                    BinaryDate = entry.Date.ToBinary(),
                    BlockOffset = entry.BlockOffset
                };
            var ptr = (RawJournalEntry*)ThisPointer;
            ptr[index] = raw;
        }

        public IEnumerator<JournalEntry> GetEnumerator()
        {
            var i = 0;
            JournalEntry entry;
            // Keep reading journal entries until _tryReadJournalEntryAt returns false.
            // incrementing i along the way.
            // That method returns false iff
            //  - the index is out of bounds
            //  - the entry is zero (indicating an early end of the list)
            while (_tryReadJournalEntryAt(i++, out entry))
                yield return entry;
        }

        public bool TryAppendEntry(JournalEntry entry)
        {
            var i = 0;
            // Keep reading journal entries until _tryReadJournalEntryAt returns false.
            // incrementing i along the way.
            // That method returns false iff
            //  - the index is out of bounds
            //  - the entry is zero (indicating an early end of the list)
            JournalEntry dummy;
            while (_tryReadJournalEntryAt(i, out dummy, suppressConstruction: true))
            {
                i++;
            }

            // at this point, we have either reached the end of the block (index >= ListCapacity) 
            // or the end of the list (with some free journal slots left)
            if (i < ListCapacity)
            {
                // i points to a free slot
                _writeJournalEntryAt(i,entry);
                OnBlockChanged();
                return true;
            }
            else
            {
                return false;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return string.Format("JournalBlock at {0}", Offset);
        }
    }
}
