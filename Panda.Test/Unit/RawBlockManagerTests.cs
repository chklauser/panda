using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Panda.Core.Blocks;
using Panda.Core.IO;
using Panda.Core.IO.InMemory;

namespace Panda.Test.Unit
{
    [TestFixture]
    public class RawBlockManagerTests : IDisposable
    {
        public const uint DefaultBlockSize = 32;

        public IRawPersistenceSpace Space;
        public IBlockManager BlockManager;
        public uint BlockCount;
        public uint BlockSize;

        public unsafe void CreateSpace(uint blockCount = 24, uint blockSize = DefaultBlockSize)
        {
            Space = new InMemorySpace(blockCount*blockSize);
            RawBlockManager.DebugBackdrop((byte*) Space.Pointer,(uint) Space.Capacity);
            RawBlockManager.Initialize(Space, BlockCount = blockCount,BlockSize = blockSize);
            BlockManager = SingleInstanceRawBlockManager.Create(Space);
        }

        public unsafe byte GetByteAt(uint blockOffset, int blockIndex)
        {
            var ptr = (byte*) Space.Pointer;
            return ptr[blockOffset * BlockSize + blockIndex];
        }

        public unsafe void SetByteAt(uint blockOffset, int blockIndex, byte value)
        {
            var ptr = (byte*)Space.Pointer;
            ptr[blockOffset * BlockSize + blockIndex] = value;
        }

        public unsafe void SetUInt32At(uint blockOffset, int uintIndex, uint value)
        {
            var ptr = (byte*)Space.Pointer;
            var uintPtr = (uint*) (ptr + BlockSize*blockOffset + sizeof (uint)*uintIndex);
            *uintPtr = value;
        }

        public unsafe uint GetUInt32At(uint blockOffset, int uintIndex)
        {
            var ptr = (byte*)Space.Pointer;
            var uintPtr = (uint*)(ptr + BlockSize * blockOffset + sizeof(uint) * uintIndex);
            return *uintPtr;
        }

        [TearDown]
        public void TearDown()
        {
            Dispose();
        }

        [Test]
        public void WriteSingleByte()
        {
            CreateSpace();

            BlockManager.WriteDataBlock((BlockOffset) 1, new byte[] {0xEF});

            Assert.That(GetByteAt(1,0),Is.EqualTo(0xEF));
        }

        [Test]
        public void ReadSingleByte()
        {
            CreateSpace();

            const byte sentinel = 0xFE;
            SetByteAt(1,0, sentinel);

            var dest = new byte[1];
            BlockManager.ReadDataBlock((BlockOffset) 1,dest);

            Assert.That(dest[0],Is.EqualTo(sentinel));
        }

        [Test]
        public void EmptyListInitialized()
        {
            CreateSpace();

            var emptyOffset = GetUInt32At(0, RawBlockManager.EmptyListFieldOffset);
            Assert.That(0 < emptyOffset && emptyOffset < BlockCount);

            for (var i = 0; i < BlockSize; i++)
                Assert.That(GetByteAt(emptyOffset, i), Is.EqualTo(0), "Empty block list byte #" + i);
        }

        [Test]
        public void RootDirectoryInitialized()
        {
            CreateSpace();

            var rootOffset = GetUInt32At(0, RawBlockManager.RootDirectoryFieldOffset);
            Assert.That(0 < rootOffset && rootOffset < BlockCount);

            for (var i = 0; i < BlockSize; i++)
                Assert.That(GetByteAt(rootOffset, i), Is.EqualTo(0), "Root directory block byte #" + i);
        }

        [Test]
        public void ReadSingleByteAtAnyPosIntoLarge(
            // Use a number of parameter combinations
            // Yes, the expressions have to be that complicated, because NUnit is very, very
            // particular about the exact argument type
            [Values(0, 3)] int destIndex, 
            [Values(0, (int) DefaultBlockSize/2-1, (int) DefaultBlockSize-1)] int blockIndex,
            [Values(4, (int) DefaultBlockSize, (int) (2u * DefaultBlockSize))] int arrayLen)
        {
            CreateSpace();

            const byte sentinel = 0xFE;
            SetByteAt(1, blockIndex, sentinel);

            var dest = new byte[arrayLen];
            BlockManager.ReadDataBlock((BlockOffset)1, dest,destIndex,blockIndex,1);

            Assert.That(dest[destIndex], Is.EqualTo(sentinel));
        }

        [Test]
        public void ReadRootDirectoryBlockOffset()
        {
            CreateSpace();

            SetUInt32At(0,RawBlockManager.RootDirectoryFieldOffset,0x15);

            Assert.That(BlockManager.RootDirectoryBlockOffset, Is.EqualTo((BlockOffset) 0x15),
                "RootBlockOffset at " + RawBlockManager.RootDirectoryFieldOffset);
        }

        [Test, ExpectedException(typeof (ArgumentNullException))]
        public void WriteDataNullCheck()
        {
            CreateSpace();

// ReSharper disable AssignNullToNotNullAttribute
            BlockManager.WriteDataBlock((BlockOffset) 1,null);
// ReSharper restore AssignNullToNotNullAttribute
        }

        [Test, ExpectedException(typeof (ArgumentException))]
        public void WriteDataTooLarge([Values(RawBlockManager.MinimumBlockSize,513u)] uint blockSize)
        {
            CreateSpace(blockSize:blockSize);

            var data = new byte[blockSize+1];
            BlockManager.WriteDataBlock((BlockOffset) 1,data);
        }

        [Test, ExpectedException(typeof (ArgumentNullException))]
        public void ReadDataNullCheck()
        {
            CreateSpace();

            BlockManager.ReadDataBlock((BlockOffset) 1,null);
        }

        [Test, ExpectedException(typeof (ArgumentOutOfRangeException))]
        public void ReadDataBlockIndexOutOfRange([Values((int) DefaultBlockSize+1,(int) DefaultBlockSize+15)] int blockIndex)
        {
            CreateSpace();

            var data = new byte[BlockSize];
            BlockManager.ReadDataBlock((BlockOffset) 1, data, blockIndex: blockIndex);
        }

        [Test]
        public void ZeroInferredForMaxBlockSizeIndex()
        {
            CreateSpace();

            var data = new byte[BlockSize];
            BlockManager.ReadDataBlock((BlockOffset)1, data, blockIndex: (int) BlockSize);
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ReadDataDestinationIndexOutOfRange([Values(0,1)] int delta)
        {
            CreateSpace();

            var data = new byte[BlockSize/2];
            BlockManager.ReadDataBlock((BlockOffset)1, data,(int) (BlockSize/2 + delta),count:1);
        }

        [Test]
        public void ZeroInferredForCount()
        {
            CreateSpace();

            var data = new byte[BlockSize / 2];
            BlockManager.ReadDataBlock((BlockOffset)1, data, (int)(BlockSize / 2));
            Assert.That(data,Is.All.EqualTo(0),"Data should not have been changed.");
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void CountLargerThanRemainingDestinationIsIllegal([Values(1,(int) DefaultBlockSize*2)] int delta)
        {
            CreateSpace();

            var data = new byte[BlockSize / 2];
            BlockManager.ReadDataBlock((BlockOffset)1, data, count: (int?) (BlockSize/2+delta));
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void CountNegativeCountIsIllegal([Values(1, (int) DefaultBlockSize*2)] int delta)
        {
            CreateSpace();

            var data = new byte[BlockSize / 2];
            BlockManager.ReadDataBlock((BlockOffset)1, data, count: -delta);
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ReadNegativeDestinationIsIllegal()
        {
            CreateSpace();

            var data = new byte[BlockSize / 2];
            BlockManager.ReadDataBlock((BlockOffset)1, data, -1);
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ReadNegativeBlockIndexIsIllegal()
        {
            CreateSpace();

            var data = new byte[BlockSize / 2];
            BlockManager.ReadDataBlock((BlockOffset)1, data, blockIndex:-1);
        }

        [Test]
        public void TotalBlockCount()
        {
            CreateSpace();

            Assert.That(BlockManager.TotalBlockCount,Is.EqualTo(BlockCount));
        }

        [Test]
        public void FullBlockReadWrite()
        {
            const string sentinel = "There is really n\0thing magical about this string.";
            var data = Encoding.UTF32.GetBytes(sentinel);

            CreateSpace(blockSize:(uint) data.Length);

            BlockManager.WriteDataBlock((BlockOffset) 2,data);

            var check = new byte[data.Length];
            BlockManager.ReadDataBlock((BlockOffset) 2,check);

            Assert.That(Encoding.UTF32.GetString(check),Is.EqualTo(sentinel));
        }

        [Test]
        public void ReadWriteShort()
        {
            const string sentinel = "There is really n\0thing magical about this string.";
            var data = Encoding.UTF32.GetBytes(sentinel);

            CreateSpace(blockSize: (uint)data.Length + 20u);

            Assert.That(BlockSize > sentinel.Length);
            BlockManager.WriteDataBlock((BlockOffset)2, data);

            var check = new byte[data.Length];
            BlockManager.ReadDataBlock((BlockOffset)2, check);

            Assert.That(Encoding.UTF32.GetString(check), Is.EqualTo(sentinel));
        }

        #region Disposal

        public void Dispose()
        {
            if (Space != null)
            {
                Space.Dispose();
                Space = null;
            }


// ReSharper disable SuspiciousTypeConversion.Global
            var disposable = BlockManager as IDisposable;
// ReSharper restore SuspiciousTypeConversion.Global

            if (disposable != null)
            {
                disposable.Dispose();
            }

            BlockManager = null;
        }

        #endregion

    }
}
