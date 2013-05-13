using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Panda.Core.Blocks;
using Panda.Core.Internal;

namespace Panda.Core.IO
{
    public class RawBlockManager : IBlockManager, IDisposable
    {
        // change the Initialize method when you add more meta fields!
        protected internal const int ServerDiskNameOffset = 8;
        // note: last time synchronized is a long, taking two uint slots
        protected internal const int LastTimeSynchronizedOffset = 6;
        protected internal const int JournalFieldOffset = 5;
        protected internal const int BreakFieldOffset = 4;
        protected internal const int EmptyListFieldOffset = 3;
        protected internal const int RootDirectoryFieldOffset = 2;
        protected internal const int BlockSizeFieldOffset = 1;
        protected internal const int BlockCountFieldOffset = 0;

        /// <summary>
        /// Currently, the meta information block at the beginning takes 20 bytes. In order to have some room to grow, we
        /// are going to go with a slightly larger minimum block size.
        /// </summary>
        public const uint MinimumBlockSize = 48;

        /// <summary>
        /// Initializes an area of memory with a recognizable pattern to help with debugging.
        /// </summary>
        /// <param name="ptr">The pointer to the start of the memory region to paint.</param>
        /// <param name="length">The length of the memory region to pain.</param>
        public static unsafe void DebugBackdrop(byte* ptr, uint length)
        {
            var end = ptr + length;

            byte counter = 15;

            // Fall back to byte-by-byte, slower but works regardless of alignment and processor word size
            for (; ptr < end; ptr++)
            {
                *ptr = (byte)(0xe0 | counter);
                unchecked
                {
                    counter++;
                }
            }
        }

        internal static unsafe void Initialize(
            [NotNull]
            IRawPersistenceSpace space,
            uint blockCount,
            uint blockSize = VirtualFileSystem.DefaultBlockSize,
            BlockOffset? rootDirectoryOffset = null,
            BlockOffset? emptyListOffset = null)
        {
            if (blockSize < MinimumBlockSize)
                throw new ArgumentOutOfRangeException("blockSize", blockSize,
                                                      "Block size must be at least " + MinimumBlockSize + ".");
            if (space == null)
                throw new ArgumentNullException("space");

            var actualRootDirectoryOffset = rootDirectoryOffset ?? (BlockOffset)1;
            var actualEmptyListOffset = emptyListOffset ?? (BlockOffset)2;

            if (actualRootDirectoryOffset.Offset >= blockCount)
                throw new ArgumentOutOfRangeException("rootDirectoryOffset", rootDirectoryOffset, "Root directory offset is beyond the end of the disk.");

            if (actualEmptyListOffset.Offset >= blockCount)
                throw new ArgumentOutOfRangeException("emptyListOffset", emptyListOffset, "Empty list offset is beyond the end of the disk.");

            var uintPtr = (uint*)space.Pointer;

            uintPtr[BlockCountFieldOffset] = blockCount;
            uintPtr[BlockSizeFieldOffset] = blockSize;
            uintPtr[RootDirectoryFieldOffset] = actualRootDirectoryOffset.Offset;
            uintPtr[EmptyListFieldOffset] = actualEmptyListOffset.Offset;
            uintPtr[BreakFieldOffset] = Math.Max(actualRootDirectoryOffset.Offset, actualEmptyListOffset.Offset) + 1;
            uintPtr[JournalFieldOffset] = 0;

            var end = ((byte*)space.Pointer) + blockSize;
            for (var bytePtr = (byte*)&uintPtr[BreakFieldOffset + 1]; bytePtr < end; bytePtr++)
                *bytePtr = 0;

            _initZero(space, blockSize, actualRootDirectoryOffset);
            _initZero(space, blockSize, actualEmptyListOffset);
        }

        private static unsafe void _initZero([NotNull] IRawPersistenceSpace space, uint blockSize, BlockOffset blockOffset)
        {
            var ptr = (byte*)space.Pointer;
            ptr = ptr + blockSize * blockOffset.Offset;
            var end = ptr + blockSize;

            // Use platform-specific pointer size when possible
            if (_isAligned(ptr) && blockSize % sizeof(void*) == 0)
            {
                var ptrEnd = (void*)end;
                for (var ptrAligned = (void**)ptr; ptrAligned < ptrEnd; ptrAligned++)
                {
                    *ptrAligned = null;
                }
            }
            else
            {
                // Fall back to byte-by-byte, slower but works regardless of alignment and processor word size
                for (; ptr < end; ptr++)
                {
                    *ptr = 0;
                }
            }
        }

        private static unsafe bool _isAligned(byte* ptr)
        {
            return ((ulong)ptr) % (ulong)sizeof(void*) == 0;
        }

        [NotNull]
        private readonly IRawPersistenceSpace _space;

        public RawBlockManager(IRawPersistenceSpace space)
        {
            _space = space;
        }

        private unsafe void* _blockAt(BlockOffset offset)
        {
            var bytePtr = (byte*)_space.Pointer;
            return bytePtr + BlockSize * offset.Offset;
        }

        #region BlockManager API

        /// <summary>
        /// Allocates a new block in the underlying persistence space
        /// and optionally initializes it to zero.
        /// </summary>
        /// <param name="leaveUninitialized">Indicates whether to leave the block uninitialized. Iff false, block will be initialized to zero.</param>
        /// <returns>The offset of the newly allocated block.</returns>
        protected BlockOffset AllocateBlock()
        {
            return AllocateBlock(false);
        }

        /// <summary>
        /// Allocates a new block in the underlying persistence space
        /// and optionally initializes it to zero.
        /// </summary>
        /// <param name="leaveUninitialized">Indicates whether to leave the block uninitialized. Iff false, block will be initialized to zero.</param>
        /// <returns>The offset of the newly allocated block.</returns>
        protected BlockOffset AllocateBlock(bool leaveUninitialized)
        {
            var head = GetEmptyListBlock(EmptyListOffset);
            BlockOffset newOffset;
            var emptyBlockCount = head.Count;
            if (emptyBlockCount == 0)
            {
                if (Break.Offset < TotalBlockCount)
                {
                    newOffset = Break;
                    Break = (BlockOffset) (Break.Offset + 1);
                    if(Space.CanGrow)
                        Space.Resize(Break.Offset*BlockSize);
                }
                else
                {
                    throw new OutofDiskSpaceException("Reached configured limit of " + TotalBlockCount + " allocated blocks.");
                }
            }
            else
            {
                newOffset = head.Remove(1)[0];
            }

            if (emptyBlockCount == 1 && head.ContinuationBlockOffset.HasValue)
            {
                // We just removed the last offset and there is a continuation of the empty list block
                // ==> see if we can free this empty list block (need to have space in the next block)
                var tailHead = GetEmptyListBlock(head.ContinuationBlockOffset.Value);
                if (tailHead.Count < tailHead.ListCapacity)
                {
                    EmptyListOffset = tailHead.Offset;
                    FreeBlock(head.Offset);
                }

                // otherwise, we just keep this empty list around for future deallocations
            }

            // For performance reasons, not all blocks (e.g. data blocks) will be initialized to zero.
            if (!leaveUninitialized)
                _initZero(Space, (uint)BlockSize, newOffset);

            OnBlockChanged(newOffset);
            return newOffset;
        }

        public virtual IDirectoryBlock AllocateDirectoryBlock()
        {
            return GetDirectoryBlock(AllocateBlock());
        }

        public virtual IDirectoryContinuationBlock AllocateDirectoryContinuationBlock()
        {
            return GetDirectoryContinuationBlock(AllocateBlock());
        }

        public virtual IFileBlock AllocateFileBlock()
        {
            return GetFileBlock(AllocateBlock());
        }

        public virtual IFileContinuationBlock AllocateFileContinuationBlock()
        {
            return GetFileContinuationBlock(AllocateBlock());
        }

        public virtual BlockOffset AllocateDataBlock()
        {
            return AllocateBlock(leaveUninitialized: true);
        }

        public virtual void FreeBlock(BlockOffset blockOffset)
        {
            if (blockOffset.Offset + 1 == Break.Offset)
            {
                // The block to be free lies next to break,
                // reduce break instead
                Break = (BlockOffset) (Break.Offset - 1u);
                if(Space.CanShrink)
                    Space.Resize(Break.Offset*BlockSize);

                // We treat a freed block as a changed block since that
                // allows us to retrieve the original version in case of
                // a rollback
                OnBlockChanged(blockOffset);
            }
            else
            {
                // The block lies within our allocated space
                // We need to track it in our empty space list.
                var head = GetEmptyListBlock(EmptyListOffset);

                // If we don't have enough space to add a new offset
                // then prepend a new empty list block.
                if (head.Count >= head.ListCapacity)
                {
                    var newHead = AllocateEmptyListBlock();
                    newHead.ContinuationBlockOffset = head.Offset;
                    EmptyListOffset = newHead.Offset;
                    head = newHead;
                }

                head.Append(new[] { blockOffset });
 
                // Here we don't need to record the block as changed since
                // it is being left as it is by free block
                // Should it be repurposed, that operation will generate
                // a journal entry.
                // This enables a relatively cheap restoration of deleted
                // files if their blocks have not been reused yet.
            }
        }

        public virtual IDirectoryBlock GetDirectoryBlock(BlockOffset blockOffset)
        {
            return new RawDirectoryBlock(this,blockOffset,(uint)BlockSize);
        }

        public virtual IDirectoryContinuationBlock GetDirectoryContinuationBlock(BlockOffset blockOffset)
        {
            return new RawDirectoryEntryListBlock(this,blockOffset,(uint) BlockSize);
        }

        public virtual IFileBlock GetFileBlock(BlockOffset blockOffset)
        {
            return new RawFileBlock(this, blockOffset, (uint)BlockSize);
        }

        public virtual IFileContinuationBlock GetFileContinuationBlock(BlockOffset blockOffset)
        {
            return new RawOffsetListBlock(this,blockOffset,(uint) BlockSize);
        }

        public virtual unsafe void WriteDataBlock(BlockOffset blockOffset, byte[] data)
        {
            WriteBlockDirect(blockOffset, data);
            OnBlockChanged(blockOffset);
        }

        public virtual unsafe void WriteBlockDirect(BlockOffset blockOffset, byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var dataBlockSize = BlockSize;

            if (data.Length > dataBlockSize)
                throw new ArgumentException(
                    string.Format("Data array is longer than block size. (block size = {0}, data length = {1})",
                        dataBlockSize, data.Length), "data");

            // Copy the provided data
            var blockAddress = _blockAt(blockOffset);
            Marshal.Copy(data, 0, (IntPtr)blockAddress, data.Length);

            // Padd rest with 0
            // (this is not really necessary, but it will help us debug)
            var start = (byte*)blockAddress;
            var end = start + dataBlockSize;
            for (var ptr = start + data.Length; ptr < end; ptr++)
                *ptr = 0;
        }

        public virtual unsafe void ReadDataBlock(BlockOffset blockOffset, byte[] destination, int destinationIndex,
                                  int blockIndex,
                                  int? count)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (destinationIndex < 0)
                throw new ArgumentOutOfRangeException("destinationIndex", destinationIndex, "Destination index cannot be negative.");
            if (blockIndex < 0)
                throw new ArgumentOutOfRangeException("blockIndex", blockIndex, "Block index cannot be negative.");

            // Check block index
            var blockRemainingLength = BlockSize - blockIndex;
            if (blockRemainingLength < 0)
                throw new ArgumentOutOfRangeException("blockIndex", blockIndex,
                    string.Format(
                        "Index into data block is beyond block boundary. (block size = {0})", BlockSize));

            // Check destination index
            var destinationRemainingLength = destination.Length - destinationIndex;
            if (destinationRemainingLength < 0)
                throw new ArgumentOutOfRangeException("destinationIndex", destinationIndex,
                    string.Format(
                        "Index into destination is beyond block boundary. (destination length = {0})",
                        destination.Length));

            // Check count
            var actualCount = count ?? Math.Min(destinationRemainingLength, blockRemainingLength);
            if (actualCount > destinationRemainingLength || actualCount > blockRemainingLength)
                throw new ArgumentOutOfRangeException("count", count,
                    "Read count is larger than either the remaining block or the remaining destination array.");
            if (actualCount < 0)
                throw new ArgumentOutOfRangeException("count", count, "Count cannot be negative.");

            // Perform copy
            var ptr = (IntPtr)_blockAt(blockOffset);
            Marshal.Copy(IntPtr.Add(ptr, blockIndex), destination, destinationIndex, actualCount);
        }

        public unsafe uint TotalBlockCount
        {
            get
            {
                var startBlock = (uint*)_space.Pointer;
                return startBlock[BlockCountFieldOffset];
            }
        }

        public uint TotalFreeBlockCount
        {
            get { return (uint) (GetEmptyListBlock(EmptyListOffset).TotalFreeBlockCount + TotalBlockCount - Break.Offset); }
        }

        public unsafe BlockOffset RootDirectoryBlockOffset
        {
            get
            {
                var startBlock = (uint*)_space.Pointer;
                return (BlockOffset)startBlock[RootDirectoryFieldOffset];
            }
        }

        public unsafe int BlockSize
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() > MinimumBlockSize);
                var startBlock = (uint*)_space.Pointer;
                return (int)startBlock[BlockSizeFieldOffset];
            }
        }

        public unsafe DateTime LastTimeSynchronized
        {
            get 
            { 
                var uintPtr = (uint*) Space.Pointer;
                var longPtr = (long*) &uintPtr[LastTimeSynchronizedOffset];
                return DateTime.FromBinary(*longPtr);
            }
        }

        public unsafe string ServerDiskName
        {
            get
            {
                var bytePtr = ServerDiskNamePointer;
                var count = *bytePtr;
                if (count == 0)
                    return null;
                bytePtr++;
                var maxCount = BlockSize - (bytePtr - (byte*) Space.Pointer);
                if (count > maxCount)
                {
                    Trace.TraceError("Illegal server disk name length {0} where only {1} bytes are available.", count,
                        maxCount);
                    return null;
                }
                var buffer = new byte[count];
                Marshal.Copy((IntPtr) bytePtr, buffer, 0, count);
                return RawDirectoryEntryListBlock.TextEncoding.GetString(buffer);
            }
            set
            {
                if (value == null)
                {
                    // Zero out the entire rest of the block
                    var end = (byte*) Space.Pointer + BlockSize;
                    for(var bytePtr = ServerDiskNamePointer; bytePtr < end; bytePtr++)
                    {
                        *bytePtr = 0;
                    }
                }
                else
                {
                    var buffer = RawDirectoryEntryListBlock.TextEncoding.GetBytes(value);
                    var bytePtr = ServerDiskNamePointer;
                    var maxCount = BlockSize - (bytePtr - (byte*)Space.Pointer);
                    if (buffer.Length > Math.Min(Byte.MaxValue, maxCount))
                    {
                        throw new ArgumentException(
                            string.Format(
                                "The server disk name is {0} bytes long. The maximum length (in bytes) is {1}.",
                                buffer.Length, maxCount));
                    }

                    *bytePtr = (byte) buffer.Length;
                    bytePtr++;
                    Marshal.Copy(buffer,0,(IntPtr)bytePtr,buffer.Length);
                }

                // the disk meta block has changed
                OnBlockChanged((BlockOffset) 0);
            }
        }

        protected unsafe byte* ServerDiskNamePointer
        {
            get
            {
                var uintPtr = (uint*) Space.Pointer;
                var bytePtr = (byte*) &uintPtr[ServerDiskNameOffset];
                return bytePtr;
            }
        }

        public unsafe void NotifySynchronized()
        {
            // clear our modified blocks cache. Will start with a clean slate from here.
            _modifiedBlocks.Clear();

            // Update last time synchronized
            var uintPtr = (uint*)Space.Pointer;
            var longPtr = (long*)&uintPtr[LastTimeSynchronizedOffset];
            *longPtr = DateTime.Now.ToBinary();
            
            // this also changes the disk meta block
            OnBlockChanged((BlockOffset) 0);
        }

        /// <summary>
        /// Indicates whether the disk is associated with a synchronization server and therefore whether
        /// it should keep a journal.
        /// </summary>
        public unsafe bool IsJournaling
        {
            get { return *ServerDiskNamePointer != 0; }
        }

        /// <summary>
        /// A temporary cache that makes sure that between two synchronization operations we
        /// only record each block change once.
        /// </summary>
        private readonly HashSet<BlockOffset> _modifiedBlocks = new HashSet<BlockOffset>();

        /// <summary>
        /// Holds blocks to be added to the journal. Do not use directly. Go through <see cref="OnBlockChanged"/> instead.
        /// </summary>
        /// <remarks><see cref="OnBlockChanged"/> sometimes triggers nested calls to itself (allocation of new journal pages). This queue collects these recursive calls.</remarks>
        private readonly Queue<BlockOffset> _journalJobQueue = new Queue<BlockOffset>();

        /// <summary>
        /// Called whenever a block has been changed. Used to write the journal.
        /// </summary>
        /// <param name="blockOffset">Offset of the block that has changed.</param>
        protected internal void OnBlockChanged(BlockOffset blockOffset)
        {
            Contract.Ensures(!IsJournaling || _journalJobQueue.Count == 0);

            // Check if journaling is even enabled
            if(!IsJournaling)
                return;

            // Between two synchronization operations don't add the same block
            // more than once.
            // This "optimization" is crucial as the journal pages
            // are being changed constantly and we only want them to appear once
            // in the journal itself.
            if(_modifiedBlocks.Contains(blockOffset))
                return;

            var alreadyRunning = _journalJobQueue.Count > 0;
            _journalJobQueue.Enqueue(blockOffset);
            _modifiedBlocks.Add(blockOffset);

            // If OnBlockChanges is already running and this is a recursive/nested call, putting
            // the block offset in the queue was enough. The activation higher up in the call stack
            // will make sure it is stored.
            if(alreadyRunning)
                return;

            // Get the latest journal block (or allocate a new journal if this 
            // disk hasn't kept a journal before)
            var nextJournalOffset = JournalOffset;
            IJournalBlock journalBlock;
            if (nextJournalOffset == null)
            {
                journalBlock = AllocateJournalBlock();
                JournalOffset = journalBlock.Offset;
            }
            else
            {
                journalBlock = GetJournalBlock(nextJournalOffset.Value);
            }

            while (_journalJobQueue.Count > 0)
            {
                var jobOffset = _journalJobQueue.Dequeue();
                var success = journalBlock.TryAppendEntry(new JournalEntry(DateTime.Now, jobOffset));
                if (!success)
                {
                    // there was not enough room for the entry in the current journal block
                    // save the entry for later and allocate more space
                    _journalJobQueue.Enqueue(jobOffset);
                    var nextBlock = AllocateJournalBlock();
                    // link new block to old list and move the journal head pointer to the new block
                    nextBlock.ContinuationBlockOffset = journalBlock.Offset;
                    JournalOffset = nextBlock.Offset;

                    // start inserting into the new journal block
                    journalBlock = nextBlock;
                }
            }
        }

        public IEnumerable<JournalEntry> GetJournalEntriesSince(DateTime lastSynchronizationTime)
        {
            var nextJournalOffset = JournalOffset;
            while (nextJournalOffset != null)
            {
                var journalBlock = GetJournalBlock(nextJournalOffset.Value);
                foreach (var entry in journalBlock.Reverse())
                {
                    if (entry.Date <= lastSynchronizationTime)
                    {
                        // found an entry that lies beyond the last synchronization time
                        // no need to look further
                        yield break;
                    }

                    // return entries in reverse order
                    yield return entry;
                }

                // see if there are more journal blocks on the stack
                nextJournalOffset = journalBlock.ContinuationBlockOffset;
            }
        }

        public void Flush()
        {
            Space.Flush();
        }

        #endregion

        protected internal IRawPersistenceSpace Space
        {
            get { return _space; }
        }

        protected unsafe BlockOffset EmptyListOffset
        {
            get
            {
                var uintPtr = (uint*)Space.Pointer;
                return (BlockOffset)uintPtr[EmptyListFieldOffset];
            }
            set
            {
                var uintPtr = (uint*)Space.Pointer;
                uintPtr[EmptyListFieldOffset] = value.Offset;

                // the disk meta block has changed
                OnBlockChanged((BlockOffset)0);
            }
        }

        protected unsafe BlockOffset? JournalOffset
        {
            get 
            { 
                var uintPtr = (uint*) Space.Pointer;
                var offset = (BlockOffset) uintPtr[JournalFieldOffset];
                if (offset.Offset == 0)
                    return null;
                else
                    return offset;
            }
            set
            {
                var uintPtr = (uint*)Space.Pointer;
                uintPtr[JournalFieldOffset] = value.HasValue ? value.Value.Offset : 0;

                // the disk meta block has changed
                OnBlockChanged((BlockOffset)0);
            }
        }

        protected unsafe BlockOffset Break
        {
            get
            {
                var uintPtr = (uint*)Space.Pointer;
                return (BlockOffset)uintPtr[BreakFieldOffset];
            }
            set
            {
                var uintPtr = (uint*)Space.Pointer;
                uintPtr[BreakFieldOffset] = value.Offset;

                // the disk meta block has changed
                OnBlockChanged((BlockOffset)0);
            }
        }

        protected virtual IEmptyListBlock AllocateEmptyListBlock()
        {
            // use the zero initialization provided by AllocateBlock for the empty list block.
            return GetEmptyListBlock(AllocateBlock());
        }

        protected virtual IJournalBlock AllocateJournalBlock()
        {
            // use the zero initialization provided by AllocateBlock for the empty list block.
            return GetJournalBlock(AllocateBlock());
        }

        protected virtual IEmptyListBlock GetEmptyListBlock(BlockOffset blockOffset)
        {
            if (blockOffset.Offset >= Break.Offset)
                throw new ArgumentOutOfRangeException("blockOffset", blockOffset, "Block offset points to location beyond allocation allocated space.");
            return new RawEmptyListBlock(this, blockOffset, (uint)BlockSize);
        }

        protected IJournalBlock GetJournalBlock(BlockOffset blockOffset)
        {
            if (blockOffset.Offset >= Break.Offset)
                throw new ArgumentOutOfRangeException("blockOffset", blockOffset,
                    "Block offset points to location beyond allocation allocated space.");

            return new RawJournalBlock(this,blockOffset, (uint) BlockSize);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_disposed)
                {
                    _space.Dispose();
                    _disposed = true;
                }
            }
        }
    }
}