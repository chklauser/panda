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

        private readonly Dictionary<int, IBlock> _blocks = new Dictionary<int, IBlock>();
        private readonly Stack<int> _freeBlockOffsets = new Stack<int>();

        // points to the next free block
        private int _spaceBreak;

        public MemBlockManager(int totalBlockCount, int rootDirectoryBlockOffset, int blockCapacity)
        {
            _blockCapacity = blockCapacity;
            TotalBlockCount = totalBlockCount;
            RootDirectoryBlockOffset = rootDirectoryBlockOffset;
            _spaceBreak = RootDirectoryBlockOffset + 1;
            for (var i = 1; i < RootDirectoryBlockOffset; i++)
                _freeBlockOffsets.Push(i);
        }

        protected virtual T Track<T>(T block) where T : IBlock
        {
            _blocks.Add(block.Offset,block);
            return block;
        }

        protected virtual int AllocateBlockOffset()
        {
            lock (_freeBlockOffsets)
            {
                if (_freeBlockOffsets.Count > 0)
                {
                    return _freeBlockOffsets.Pop();
                }

                if (_spaceBreak < TotalBlockCount)
                    return _spaceBreak++;
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

        public void FreeBlock(int blockOffset)
        {
            IBlock block;
            if (_blocks.TryGetValue(blockOffset,out block))
            {
                ((MemBlock) block).IsAllocated = false;
                _blocks.Remove(blockOffset);
                if (blockOffset == _spaceBreak - 1)
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

        public T GetBlock<T>(int blockOffset) where T : class, IBlock
        {
            IBlock block;
            if (_blocks.TryGetValue(blockOffset, out block))
            {
                Assert.IsAssignableFrom<T>(block,"The block at offset {0} does not have the expected type. If this were a real file system, you'd be dead now.",blockOffset);
                return (T) block;
            }
            else
            {
                throw new KeyNotFoundException("The block offset " + blockOffset + " does not exist. If this were a real file system, you'd be dead now.");
            }
        }

        public IDirectoryBlock GetDirectoryBlock(int blockOffset)
        {
            return GetBlock<IDirectoryBlock>(blockOffset);
        }

        public IDirectoryContinuationBlock GetDirectoryContinuationBlock(int blockOffset)
        {
            return GetBlock<IDirectoryContinuationBlock>(blockOffset);
        }

        public IFileBlock GetFileBlock(int blockOffset)
        {
            return GetBlock<IFileBlock>(blockOffset);
        }

        public IFileContinuationBlock GetFileContinuationBlock(int blockOffset)
        {
            return GetBlock<IFileContinuationBlock>(blockOffset);
        }

        public int TotalBlockCount { get; private set; }
        public int RootDirectoryBlockOffset { get; private set; }

        public int BlockCapacity
        {
            get { return _blockCapacity; }
        }
    }
}