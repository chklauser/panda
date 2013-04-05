using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Panda.Core.Blocks;

namespace Panda.Core.Internal
{
    class VirtualFileOpenStream : System.IO.Stream
    {
        private readonly VirtualDiskImpl _disk;

        /// <summary>
        /// Block offset of the original (=first) file block
        /// </summary>
        private readonly BlockOffset _blockOffset;

        /// <summary>
        /// Number of bytes read from file until now
        /// </summary>
        private long _bytesRead;

        /// <summary>
        /// Current file continuation block where the stream should read
        /// </summary>
        private IFileContinuationBlock _currentFileContinuationBlock;

        private int _currentNumberOfDataBlockOffsetInFileContinuationBlock;

        /// <summary>
        /// Current block offset in data block
        /// </summary>
        private int _currentDataBlockOffset;

        public VirtualFileOpenStream(VirtualDiskImpl disk, BlockOffset blockOffset)
        {
            _disk = disk;
            _blockOffset = blockOffset;
            _bytesRead = 0;
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            // read one block and return it if count larger than block size

            // TODO: Cache toArray()
            BlockOffset dataBlockOffset =
                _currentFileContinuationBlock.ToArray()[_currentNumberOfDataBlockOffsetInFileContinuationBlock];

            int numberOfBytesRead;

            if (count + _currentDataBlockOffset <= _disk.BlockManager.DataBlockSize)
            {
                numberOfBytesRead = count;
                _disk.BlockManager.ReadDataBlock(dataBlockOffset, buffer, _currentDataBlockOffset, numberOfBytesRead);
                _currentDataBlockOffset = count;
            }
            else
            {
                numberOfBytesRead = count % _disk.BlockManager.DataBlockSize - _currentDataBlockOffset;
                _disk.BlockManager.ReadDataBlock(dataBlockOffset, buffer, _currentDataBlockOffset, numberOfBytesRead);
                _currentDataBlockOffset = 0;
            }

            _bytesRead += numberOfBytesRead;

            return _movePointersToNextDataBlockOffset() ? numberOfBytesRead : 0; 
        }

        /// <summary>
        /// Moves all pointers to the next data block offset. Returns false if at end of file.
        /// </summary>
        /// <returns>False if at end of file</returns>
        private bool _movePointersToNextDataBlockOffset()
        {
            // check if already on end of file continuation block
            if (_currentNumberOfDataBlockOffsetInFileContinuationBlock >= _currentFileContinuationBlock.Count - 1)
            {
                // is there a next file continuation block?
                if (_currentFileContinuationBlock.ContinuationBlockOffset.HasValue)
                {
                    // set pointers to next file continuation block
                    _currentFileContinuationBlock =
                        _disk.BlockManager.GetFileContinuationBlock(
                            _currentFileContinuationBlock.ContinuationBlockOffset.Value);
                    _currentNumberOfDataBlockOffsetInFileContinuationBlock = 0;
                    return true;
                }
                else
                {
                    // at end of file
                    return false;
                }
            }
            else
            {
                _currentNumberOfDataBlockOffsetInFileContinuationBlock += 1;
                return true;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _disk.BlockManager.GetFileBlock(_blockOffset).Size; }
        }

        public override long Position
        {
            get { return _bytesRead; }
            set { throw new NotSupportedException(); }
        }
    }
}
