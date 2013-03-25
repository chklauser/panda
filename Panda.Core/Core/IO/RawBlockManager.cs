using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Panda.Core.Blocks;

namespace Panda.Core.IO
{
    public class RawBlockManager : IBlockManager
    {
        // change the Initialize method when you add more meta fields!
        protected internal const int BreakFieldOffset = 4;
        protected internal const int EmptyListFieldOffset = 3;
        protected internal const int RootDirectoryFieldOffset = 2;
        protected internal  const int BlockSizeFieldOffset = 1;
        protected internal  const int BlockCountFieldOffset = 0;
        
        protected internal const uint DefaultBlockSize = 4096;

        /// <summary>
        /// Currently, the meta information block at the beginning takes 20 bytes. In order to have some room to grow, we
        /// are going to go with a slightly larger minimum block size.
        /// </summary>
        public const uint MinimumBlockSize = 32;

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
                *ptr = (byte) (0xe0 | counter);
                unchecked
                {
                    counter++;
                }
            }
        }

        public static unsafe void Initialize(
            [NotNull]
            IRawPersistenceSpace space,
            uint blockCount,
            uint blockSize = DefaultBlockSize,
            BlockOffset? rootDirectoryOffset = null,
            BlockOffset? emptyListOffset = null)
        {
            if (blockSize < MinimumBlockSize)
            {
                throw new ArgumentOutOfRangeException("blockSize",blockSize,"Block size must be at least " + MinimumBlockSize + ".");
            }

            var actualRootDirectoryOffset = rootDirectoryOffset ?? (BlockOffset)1;
            var actualEmptyListOffset = emptyListOffset ?? (BlockOffset) 2;

            if(actualRootDirectoryOffset.Offset >= blockCount)
                throw new ArgumentOutOfRangeException("rootDirectoryOffset",rootDirectoryOffset,"Root directory offset is beyond the end of the disk.");

            if(actualEmptyListOffset.Offset >= blockCount)
                throw new ArgumentOutOfRangeException("emptyListOffset",emptyListOffset,"Empty list offset is beyond the end of the disk.");

            var uintPtr = (uint*) space.Pointer;

            uintPtr[BlockCountFieldOffset] = blockCount;
            uintPtr[BlockSizeFieldOffset] = blockSize;
            uintPtr[RootDirectoryFieldOffset] = actualRootDirectoryOffset.Offset;
            uintPtr[EmptyListFieldOffset] = actualEmptyListOffset.Offset;
            uintPtr[BreakFieldOffset] = Math.Max(actualRootDirectoryOffset.Offset, actualEmptyListOffset.Offset) + 1;

            var end = ((byte*) space.Pointer) + blockSize;
            for (var bytePtr = (byte*)&uintPtr[BreakFieldOffset + 1]; bytePtr < end; bytePtr++)
                *bytePtr = 0;

            _initZero(space, blockSize,actualRootDirectoryOffset);
            _initZero(space, blockSize, actualEmptyListOffset);
        }

        private static unsafe void _initZero([NotNull] IRawPersistenceSpace space, uint blockSize, BlockOffset blockOffset)
        {
            var ptr = (byte*) space.Pointer;
            ptr = ptr + blockSize*blockOffset.Offset;
            var end = ptr + blockSize;

            // Use platform-specific pointer size when possible
            if (_isAligned(ptr) &&  blockSize%sizeof(void*) == 0)
            {
                var ptrEnd = (void*) end;
                for (var ptrAligned = (void**) ptr; ptrAligned < ptrEnd; ptrAligned++)
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
            return ((ulong)ptr)%(ulong)sizeof(void*) == 0;
        }

        [NotNull]
        private readonly IRawPersistenceSpace _space;

        public RawBlockManager(IRawPersistenceSpace space)
        {
            _space = space;
        }

        private unsafe void* _blockAt(BlockOffset offset)
        {
            var bytePtr = (byte*) _space.Pointer;
            return bytePtr + BlockSize*offset.Offset;
        }

        public int BlockSize
        {
            get { return DataBlockSize; }
        }

        #region BlockManager API

        public IDirectoryBlock AllocateDirectoryBlock()
        {
            throw new NotImplementedException();
        }

        public IDirectoryContinuationBlock AllocateDirectoryContinuationBlock()
        {
            throw new NotImplementedException();
        }

        public IFileBlock AllocateFileBlock()
        {
            throw new NotImplementedException();
        }

        public IFileContinuationBlock AllocateFileContinuationBlock()
        {
            throw new NotImplementedException();
        }

        public BlockOffset AllocateDataBlock()
        {
            throw new NotImplementedException();
        }

        public void FreeBlock(BlockOffset blockOffset)
        {
            throw new NotImplementedException();
        }

        public IDirectoryBlock GetDirectoryBlock(BlockOffset blockOffset)
        {
            throw new NotImplementedException();
        }

        public IDirectoryContinuationBlock GetDirectoryContinuationBlock(BlockOffset blockOffset)
        {
            throw new NotImplementedException();
        }

        public IFileBlock GetFileBlock(BlockOffset blockOffset)
        {
            throw new NotImplementedException();
        }

        public IFileContinuationBlock GetFileContinuationBlock(BlockOffset blockOffset)
        {
            throw new NotImplementedException();
        }

        public unsafe void WriteDataBlock(BlockOffset blockOffset, byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var dataBlockSize = DataBlockSize;

            if (data.Length > dataBlockSize)
                throw new ArgumentException(
                    string.Format("Data array is longer than block size. (block size = {0}, data length = {1})",
                        dataBlockSize, data.Length), "data");

            // Copy the provided data
            var blockAddress = _blockAt(blockOffset);
            Marshal.Copy(data,0,(IntPtr)blockAddress,data.Length);

            // Padd rest with 0
            // (this is not really necessary, but it will help us debug)
            var start = (byte*) blockAddress;
            var end = start + dataBlockSize;
            for (var ptr = start + data.Length; ptr < end; ptr++)
                *ptr = 0;
        }

        public unsafe void ReadDataBlock(BlockOffset blockOffset, byte[] destination, int destinationIndex = 0,
                                  int blockIndex = 0,
                                  int? count = null)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");
            if(destinationIndex < 0)
                throw new ArgumentOutOfRangeException("destinationIndex",destinationIndex,"Destination index cannot be negative.");
            if(blockIndex < 0)
                throw new ArgumentOutOfRangeException("blockIndex",blockIndex,"Block index cannot be negative.");

            // Check block index
            var blockRemainingLength = BlockSize - blockIndex;
            if (blockRemainingLength < 0)
                throw new ArgumentOutOfRangeException("blockIndex",blockIndex,
                    string.Format(
                        "Index into data block is beyond block boundary. (block size = {0})",BlockSize));
            
            // Check destination index
            var destinationRemainingLength = destination.Length - destinationIndex;
            if (destinationRemainingLength < 0)
                throw new ArgumentOutOfRangeException("destinationIndex",destinationIndex,
                    string.Format(
                        "Index into destination is beyond block boundary. (destination length = {0})",
                        destination.Length));

            // Check count
            var actualCount = count ?? Math.Min(destinationRemainingLength, blockRemainingLength);
            if (actualCount > destinationRemainingLength || actualCount > blockRemainingLength)
                throw new ArgumentOutOfRangeException("count", count,
                    "Read count is larger than either the remaining block or the remaining destination array.");
            if(actualCount < 0)
                throw new ArgumentOutOfRangeException("count",count,"Count cannot be negative.");

            // Perform copy
            var ptr = (IntPtr)_blockAt(blockOffset);
            Marshal.Copy(IntPtr.Add(ptr,blockIndex),destination,destinationIndex, actualCount);
        }

        public unsafe uint TotalBlockCount
        {
            get
            {
                var startBlock = (uint*)_space.Pointer;
                return startBlock[BlockCountFieldOffset];
            }
        }

        public unsafe BlockOffset RootDirectoryBlockOffset
        {
            get
            {
                var startBlock = (uint*)_space.Pointer;
                return (BlockOffset) startBlock[RootDirectoryFieldOffset];
            }
        }

        public unsafe int DataBlockSize
        {
            get
            {
                var startBlock = (uint*) _space.Pointer;
                return (int) startBlock[BlockSizeFieldOffset];
            }
        }

        #endregion

        protected IRawPersistenceSpace Space
        {
            get { return _space; }
        }

    }
}