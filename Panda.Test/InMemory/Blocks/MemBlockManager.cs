using System;
using System.Collections.Generic;
using NUnit.Framework;
using Panda.Core;
using Panda.Core.Blocks;

namespace Panda.Test.InMemory.Blocks
{
    public class MemBlockManager : IBlockManager
    {
        /// <summary>
        /// The capacity of individual blocks (how many entries fit into one block etc.)
        /// </summary>
        private readonly int _blockCapacity;

        class MemStored
        {
            public IBlock Block;
            public  byte[] Data;
        }

        private readonly Dictionary<BlockOffset, MemStored> _blocks = new Dictionary<BlockOffset, MemStored>();
        private readonly Stack<BlockOffset> _freeBlockOffsets = new Stack<BlockOffset>();

        // points to the next free block
        private uint _spaceBreak;

        public MemBlockManager(int totalBlockCount, BlockOffset rootDirectoryBlockOffset, int blockCapacity)
        {
            _blockCapacity = blockCapacity;
            TotalBlockCount = totalBlockCount;
            RootDirectoryBlockOffset = rootDirectoryBlockOffset;
            _spaceBreak = RootDirectoryBlockOffset.Offset + 1;
            for (var i = 1u; i < RootDirectoryBlockOffset.Offset; i++)
                _freeBlockOffsets.Push((BlockOffset) i);
        }

        protected virtual T Track<T>(T block) where T : IBlock
        {
            _blocks.Add(block.Offset,new MemStored{Block = block});
            return block;
        }

        protected virtual BlockOffset AllocateBlockOffset()
        {
            lock (_freeBlockOffsets)
            {
                if (_freeBlockOffsets.Count > 0)
                {
                    return _freeBlockOffsets.Pop();
                }

                if (_spaceBreak < TotalBlockCount)
                    return (BlockOffset) _spaceBreak++;
                else
                    throw new OutofDiskSpaceException();
            }
        }

        public IDirectoryBlock AllocateDirectoryBlock()
        {
            return Track(new MemDirectoryBlock(AllocateBlockOffset(),BlockCapacity));
        }

        public IDirectoryContinuationBlock AllocateDirectoryContinuationBlock()
        {
            return Track(new MemDirectoryContinuationBlock(AllocateBlockOffset(),BlockCapacity));
        }

        public IFileBlock AllocateFileBlock()
        {
            return Track(new MemFileBlock(AllocateBlockOffset(),BlockCapacity));
        }

        public IFileContinuationBlock AllocateFileContinuationBlock()
        {
            return Track(new MemOffsetList(AllocateBlockOffset(),BlockCapacity));
        }

        public int AllocateDataBlock()
        {
            throw new NotImplementedException();
        }

        public void FreeBlock(BlockOffset blockOffset)
        {
            MemStored mem;
            if (_blocks.TryGetValue(blockOffset,out mem))
            {
                if (mem.Block != null)
                {
                    ((MemBlock) mem.Block).IsAllocated = false;
                }
                _blocks.Remove(blockOffset);
                if (blockOffset.Offset == _spaceBreak - 1)
                {
                    _spaceBreak--;
                }
                else
                {
                    _freeBlockOffsets.Push(blockOffset);
                }
            }
            else
            {
                throw new KeyNotFoundException("The block offset " + blockOffset + " does not exist. If this were a real file system, you'd be dead now.");
            }
        }

        public T GetBlock<T>(BlockOffset blockOffset) where T : class, IBlock
        {
            IBlock block;
            MemStored mem;
            if (_blocks.TryGetValue(blockOffset, out mem) && (block = mem.Block) != null)
            {
                Assert.IsAssignableFrom<T>(block,"The block at offset {0} does not have the expected type. If this were a real file system, you'd be dead now.",blockOffset);
                return (T) block;
            }
            else if (mem != null)
            {
                // found data block when we expected file system block. We cannot detect this on the real disk.
                throw new KeyNotFoundException("The block offset " + blockOffset + " refers to a data block. If this were a real file system, you'd be very dead now.");
            }
            else
            {
                throw new KeyNotFoundException("The block offset " + blockOffset + " does not exist. If this were a real file system, you'd be dead now.");
            }
        }

        public IDirectoryBlock GetDirectoryBlock(BlockOffset blockOffset)
        {
            return GetBlock<IDirectoryBlock>(blockOffset);
        }

        public IDirectoryContinuationBlock GetDirectoryContinuationBlock(BlockOffset blockOffset)
        {
            return GetBlock<IDirectoryContinuationBlock>(blockOffset);
        }

        public IFileBlock GetFileBlock(BlockOffset blockOffset)
        {
            return GetBlock<IFileBlock>(blockOffset);
        }

        public IFileContinuationBlock GetFileContinuationBlock(BlockOffset blockOffset)
        {
            return GetBlock<IFileContinuationBlock>(blockOffset);
        }

        public void WriteDataBlock(BlockOffset blockOffset, byte[] data)
        {
            throw new NotImplementedException();
        }

        public int TotalBlockCount { get; private set; }
        public BlockOffset RootDirectoryBlockOffset { get; private set; }

        public int DataBlockSize
        {
            get { throw new NotImplementedException(); }
        }

        public int BlockCapacity
        {
            get { return _blockCapacity; }
        }
    }
}