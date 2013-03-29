using System;
using System.Collections.Generic;
using Panda.Core;
using Panda.Core.Blocks;

namespace Panda.Test.InMemory.Blocks
{
    public class MemBlockManager : IBlockManager
    {
        /// <summary>
        /// The capacity of individual blocks (how many entries fit into one block etc.)
        /// </summary>
        private readonly int _metaBlockCapacity;

        /// <summary>
        /// The capacity of individual data blocks in number of bytes.
        /// </summary>
        /// <remarks>In reality, the file system block capacity is closely linked to the data block capacity, but for this mock, it doesn't matter.</remarks>
        private readonly int _dataBlockCapacity;

        class MemStored
        {
            public IBlock Block;
            public byte[] Data;
        }

        private readonly Dictionary<BlockOffset, MemStored> _blocks = new Dictionary<BlockOffset, MemStored>();
        private readonly Stack<BlockOffset> _freeBlockOffsets = new Stack<BlockOffset>();

        // points to the next free block
        private uint _spaceBreak;

        public MemBlockManager(uint totalBlockCount, BlockOffset rootDirectoryBlockOffset, int metaBlockCapacity, int dataBlockCapacity)
        {
            _metaBlockCapacity = metaBlockCapacity;
            _dataBlockCapacity = dataBlockCapacity;
            TotalBlockCount = totalBlockCount;
            RootDirectoryBlockOffset = rootDirectoryBlockOffset;
            _spaceBreak = RootDirectoryBlockOffset.Offset + 1;
            for (var i = 1u; i < RootDirectoryBlockOffset.Offset; i++)
                _freeBlockOffsets.Push((BlockOffset) i);

            _trackBlock(new MemDirectoryBlock(RootDirectoryBlockOffset, MetaBlockCapacity));
        }

        protected virtual T Track<T>(T block) where T : IBlock
        {
            return _trackBlock(block);
        }

        private T _trackBlock<T>(T block) where T : IBlock
        {
            _blocks.Add(block.Offset, new MemStored {Block = block});
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

        public virtual IDirectoryBlock AllocateDirectoryBlock()
        {
            return Track(new MemDirectoryBlock(AllocateBlockOffset(),MetaBlockCapacity));
        }

        public virtual IDirectoryContinuationBlock AllocateDirectoryContinuationBlock()
        {
            return Track(new MemDirectoryContinuationBlock(AllocateBlockOffset(),MetaBlockCapacity));
        }

        public virtual IFileBlock AllocateFileBlock()
        {
            return Track(new MemFileBlock(AllocateBlockOffset(),MetaBlockCapacity));
        }

        public virtual IFileContinuationBlock AllocateFileContinuationBlock()
        {
            return Track(new MemOffsetList(AllocateBlockOffset(),MetaBlockCapacity));
        }

        public virtual BlockOffset AllocateDataBlock()
        {
            var offset = AllocateBlockOffset();
            var dataBlock = new byte[DataBlockSize];
            _blocks.Add(offset,new MemStored {Data = dataBlock});
            return offset;
        }

        public virtual void FreeBlock(BlockOffset blockOffset)
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

        internal T GetBlock<T>(BlockOffset blockOffset) where T : class, IBlock
        {
            IBlock block;
            MemStored mem;
            if (_blocks.TryGetValue(blockOffset, out mem) && (block = mem.Block) != null)
            {
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

        public virtual IDirectoryBlock GetDirectoryBlock(BlockOffset blockOffset)
        {
            return GetBlock<IDirectoryBlock>(blockOffset);
        }

        public virtual IDirectoryContinuationBlock GetDirectoryContinuationBlock(BlockOffset blockOffset)
        {
            return GetBlock<IDirectoryContinuationBlock>(blockOffset);
        }

        public virtual IFileBlock GetFileBlock(BlockOffset blockOffset)
        {
            return GetBlock<IFileBlock>(blockOffset);
        }

        public virtual IFileContinuationBlock GetFileContinuationBlock(BlockOffset blockOffset)
        {
            return GetBlock<IFileContinuationBlock>(blockOffset);
        }

        public virtual void WriteDataBlock(BlockOffset blockOffset, byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            
            MemStored store;
            byte[] block;
            if (_blocks.TryGetValue(blockOffset, out store) && (block = store.Data) != null)
            {
                Array.Copy(data,0,block,0,data.Length);
                for (var i = data.Length; i < block.Length; i++)
                    data[i] = 0;
            }
            else
            {
                throw new InvalidOperationException("There is no data block at offset " + blockOffset + " to write to. If this were a real file system, you'd be dead now.");
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "This is an internal API that we do not expect non-C#, non-VB.NET, non-F#, non-C++/CLI code to call. Therefore, using default values is unproblematic.")]
        public void ReadDataBlock(BlockOffset blockOffset, byte[] destination, int destinationIndex = 0, int blockIndex = 0,
                                  int? count = null)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");
            
            MemStored store;
            byte[] block;
            if (_blocks.TryGetValue(blockOffset, out store) && (block = store.Data) != null)
            {
                Array.Copy(block, blockIndex, destination, destinationIndex,
                    count ?? Math.Min(block.Length - blockIndex, destination.Length - destinationIndex));
            }
            else
            {
                throw new InvalidOperationException("There is no data block at offset " + blockOffset + " to read from. If this were a real file system, you'd be dead now.");
            }
        }

        public uint TotalBlockCount { get; private set; }
        public BlockOffset RootDirectoryBlockOffset { get; private set; }

        public int DataBlockSize
        {
            get { return _dataBlockCapacity; }
        }

        public int MetaBlockCapacity
        {
            get { return _metaBlockCapacity; }
        }

        public int DataBlockCapacity
        {
            get { return _dataBlockCapacity; }
        }
    }
}