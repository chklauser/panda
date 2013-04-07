using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Panda.Core.Blocks;
using Panda.Core.Internal;

namespace Panda.Core.IO
{
    internal class RawDirectoryEntryListBlock : RawContinuedBlock, IDirectoryContinuationBlock
    {
        public static readonly Encoding TextEncoding = Encoding.UTF8;

        public RawDirectoryEntryListBlock(IRawPersistenceSpace space, BlockOffset offset, uint blockSize) : base(space, offset, blockSize)
        {
        }

        protected virtual int MetaPrefixUInt32Count
        {
            get { return 0; }
        }

        public unsafe IEnumerator<DirectoryEntry> GetEnumerator()
        {
            var ptr = (byte*) ThisPointer;
            ptr += sizeof (UInt32)*MetaPrefixUInt32Count;
            var end = ptr + (BlockSize - sizeof (BlockOffset));
            return new DirectoryEntryEnumerator(ptr,end);
        }

        private static unsafe int _constantDirectoryEntrySize
        {
            get { return sizeof (byte) + sizeof (DirectoryEntryFlags) + sizeof (BlockOffset); }
        }


        private unsafe class DirectoryEntryEnumerator : IEnumerator<DirectoryEntry>
        {
            #region Disposal (not used, but inherited from IEnumerator`1)

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {

                }
            }

            ~DirectoryEntryEnumerator()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            #endregion

            private readonly byte[] _buffer = new byte[Byte.MaxValue];
            private byte* _ptr;
            private readonly byte* _end;

            public DirectoryEntryEnumerator(byte* ptr, byte* end)
            {
                if(ptr > end)
                    throw new ArgumentOutOfRangeException("end","end is before (or at) ptr.");
                _ptr = ptr;
                _end = end;
            }


            public bool MoveNext()
            {
                if (_ptr + _constantDirectoryEntrySize >= _end)
                    return false;

                byte nameLen = *_ptr;

                // zero byte indicates end of list
                if (nameLen == 0)
                    return false;

                var flags = *(DirectoryEntryFlags*)(&_ptr[1]);
                var offset = *(BlockOffset*)(&_ptr[2]);

                // Check if the directory entry stays within the bounds of the block
                // If this condition is violated, the disk is probably corrupt.
                var nextPtr = _ptr + _constantDirectoryEntrySize + nameLen;
                if (nextPtr > _end)
                    throw new PandaException("Invalid directory entry. Name length exceeds block boundaries.");

                Marshal.Copy((IntPtr) (_ptr + _constantDirectoryEntrySize), _buffer, 0, nameLen);
                Current = new DirectoryEntry(TextEncoding.GetString(_buffer, 0, nameLen), offset, flags);
                _ptr = nextPtr;
                return true;
            }

            public void Reset()
            {
                throw new NotSupportedException("This directory entry enumerator does not support resetting.");
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public DirectoryEntry Current { get; protected set; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get
            {
                // This isn't a terribly efficient implementation of count
                // as it (needlessly) decodes strings in entries as opposed
                // to just counting the entries.
                return this.Count();
            }
        }

        public void DeleteEntry(DirectoryEntry entry)
        {
            if (!TryWriteEntries(this.Where(x => !entry.Equals(x))))
            {
                throw new PandaException("Error while deleting an entry. Surviving entries no longer fit into blocks.");
            }
        }

        public bool TryAddEntry(DirectoryEntry entry)
        {
            return TryWriteEntries(this.Append(entry));
        }

        protected unsafe bool TryWriteEntries(IEnumerable<DirectoryEntry> entries)
        {
            var prefixByteSize = MetaPrefixUInt32Count*sizeof (UInt32);
            // Buffer just for directory entries
            var buffer = new byte[BlockSize-prefixByteSize-sizeof(UInt32)];

            var index = 0;
            foreach (var entry in entries)
            {
                var encoded = TextEncoding.GetBytes(entry.Name);
                if(encoded.Length > Byte.MaxValue)
                    throw new ArgumentOutOfRangeException("entries",encoded.Length,
                                                          String.Format("Length of the directory entry {0}exceeds maximum of {1}.", entry, Byte.MaxValue));

                buffer[index] = (byte) encoded.Length;
                buffer[index + 1] = (byte) entry.Flags;
                
                var theOffset = entry.BlockOffset;
                Marshal.Copy((IntPtr)(&theOffset),buffer,index+_constantDirectoryEntrySize-sizeof(BlockOffset),sizeof(BlockOffset));
                
                var entryLen = _constantDirectoryEntrySize + encoded.Length;
                if (buffer.Length - index - entryLen <= 0)
                {
                    // Not enough space for this entry, aborting the write.
                    return false;
                }

                Array.Copy(encoded,0,buffer,index+_constantDirectoryEntrySize,encoded.Length);

                index += entryLen;
            }

            var ptr = ((byte*) ThisPointer) + prefixByteSize;
            Marshal.Copy(buffer,0,(IntPtr)ptr,buffer.Length);
            return true;
        }
    }

    class RawDirectoryBlock : RawDirectoryEntryListBlock, IDirectoryBlock
    {
        public RawDirectoryBlock(IRawPersistenceSpace space, BlockOffset offset, uint blockSize) : base(space, offset, blockSize)
        {
        }

        protected override int MetaPrefixUInt32Count
        {
            get
            {
                return base.MetaPrefixUInt32Count+1;
            }
        }

        protected unsafe long* TotalSizeSlot
        {
            get { return (long*) ((byte*)ThisPointer + (base.MetaPrefixUInt32Count*sizeof (UInt32))); }
        }

        public unsafe long TotalSize
        {
            get { return *TotalSizeSlot; }
            set
            {
                if(value < 0)
                    throw new ArgumentOutOfRangeException("value",value,"TotalSize cannot be negative.");
                *TotalSizeSlot = value;
            }
        }
    }
}