using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Panda.Core.Blocks;

namespace Panda.Core.Internal
{
    class VirtualFileOpenStream : System.IO.Stream
    {
        [NotNull] private readonly VirtualDiskImpl _disk;

        /// <summary>
        /// Block offset of the original (=first) file block
        /// </summary>
        [NotNull] private readonly IFileBlock _file;

        /// <summary>
        /// Number of bytes read from file until now
        /// </summary>
        private long _bytesRead;

        /// <summary>
        /// Current file continuation block where the stream should read
        /// </summary>
        [CanBeNull] private IFileContinuationBlock _currentFileBlock;

        /// <summary>
        /// A queue that holds block offsets that we have read from the current file (continuation) block ahead of time
        /// </summary>
        [NotNull]
        private readonly Queue<BlockOffset> _offsetBuffer = new Queue<BlockOffset>();

        [CanBeNull] private BlockOffset? _currentDataBlock = null;

        /// <summary>
        /// Current block offset in data block
        /// </summary>
        private int _currentOffsetIntoDataBlock;

        public VirtualFileOpenStream([NotNull] VirtualDiskImpl disk, [NotNull] IFileBlock file)
        {
            if (file == null)
                throw new ArgumentNullException("file");
            if (disk == null)
                throw new ArgumentNullException("disk");
            
            _disk = disk;
            _file = file;

            _currentFileBlock = _file;
            _bytesRead = 0;

            // Make sure the buffer starts out initialized
            _moveToNextContinuationBlock();
        }

        /// <summary>
        /// Copies the data block offsets of the <see cref="_currentFileBlock"/> to the <see cref="_offsetBuffer"/>. Signals if there are potentially more continuation blocks.
        /// </summary>
        /// <returns>True if it makes sense to call the method again; false otherwise</returns>
        private bool _moveToNextContinuationBlock()
        {
            // It must be safe to call this method, even if it has returned false before.
            if (_currentFileBlock == null)
                return false;

            // Read the entire offset list into our buffer
            foreach (var dataBlockOffset in _currentFileBlock)
                _offsetBuffer.Enqueue(dataBlockOffset);

            // See if it makes sense to have _moveNext called again
            if (_currentFileBlock.ContinuationBlockOffset != null)
            {
                _currentFileBlock = _disk.BlockManager.GetFileContinuationBlock(_currentFileBlock.ContinuationBlockOffset.Value);
            }
            else
            {
                _currentFileBlock = null;
            }

            return true;
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
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if(count > buffer.Length-offset)
                throw new ArgumentOutOfRangeException("count",count,"Not enough space in array for that number of bytes.");

            return _read(buffer, offset, count);
        }

        private int _read(byte[] buffer, int offset, int count)
        {
            if (_currentDataBlock.HasValue)
            {
                // supply from current data block, don't go over "edge" 
                // of this block. We'd rather just return less than what
                // the client asked for.

                var bytesLeftInBlock = _disk.BlockManager.DataBlockSize - _currentOffsetIntoDataBlock;
                Debug.Assert(bytesLeftInBlock > 0);

                // limit effective count by 1) bytes left in current data block and 2) bytes left in (logical) file
                var effectiveCount = Math.Min(bytesLeftInBlock, count);
                // a min-operation with an int cannot be larger than an int
                effectiveCount = (int) Math.Min(effectiveCount, Length - _bytesRead);

                // Have the block manager copy the data for us
                _disk.BlockManager.ReadDataBlock(_currentDataBlock.Value, buffer, offset, _currentOffsetIntoDataBlock,
                    effectiveCount);

                // Update counters
                _currentOffsetIntoDataBlock += effectiveCount;
                _bytesRead += effectiveCount;

                if (_currentOffsetIntoDataBlock >= _disk.BlockManager.DataBlockSize)
                {
                    Debug.Assert(_currentOffsetIntoDataBlock == _disk.BlockManager.DataBlockSize);
                    // we have read to the end of this data block, mark it as read by forgetting about it
                    // it has already been removed from the queue/buffer
                    _currentDataBlock = null;
                }
                return effectiveCount;
            }
                // we first need to get our hands on a data block.
                // Check buffer first
            else if (_offsetBuffer.Count > 0)
            {
                _currentDataBlock = _offsetBuffer.Dequeue();
                _currentOffsetIntoDataBlock = 0;

                // and then proceed as before
                return _read(buffer, offset, count);
            }
            else
            {
                // see if we can get more data block offsets into our buffer
                if (_moveToNextContinuationBlock())
                {
                    // offset buffer now might contain more entries
                    // proceed as before
                    return _read(buffer, offset, count);
                }
                else
                {
                    // we can't get our hands on any additional blocks
                    // zero signals that the we have reached EOF.
                    return 0;
                }
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
            get { return _file.Size; }
        }

        public override long Position
        {
            get { return _bytesRead; }
            set { throw new NotSupportedException(); }
        }
    }
}
