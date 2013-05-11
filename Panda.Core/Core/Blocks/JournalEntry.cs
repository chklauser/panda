using System;

namespace Panda.Core.Blocks
{
    /// <summary>
    /// Tuple used in <see cref="IJournalBlock"/>s to represent a known change in a block at a certain time.
    /// </summary>
    public class JournalEntry : IEquatable<JournalEntry>, IComparable<JournalEntry>
    {
        private readonly DateTime _date;
        private readonly BlockOffset _blockOffset;

        /// <summary>
        /// The time and date on which the block was changed.
        /// </summary>
        public DateTime Date
        {
            get { return _date; }
        }

        /// <summary>
        /// The offset of the block that was changed.
        /// </summary>
        public BlockOffset BlockOffset
        {
            get { return _blockOffset; }
        }

        public JournalEntry(DateTime date, BlockOffset blockOffset)
        {
            _date = date;
            _blockOffset = blockOffset;
        }

        public bool Equals(JournalEntry other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _date.Equals(other._date) && _blockOffset.Equals(other._blockOffset);
        }

        public int CompareTo(JournalEntry other)
        {
            if (other == null)
                return 1;

            var dc = _date.CompareTo(other._date);
            return dc != 0 
                ? dc 
                : _blockOffset.Offset.CompareTo(other._blockOffset.Offset);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((JournalEntry) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_date.GetHashCode()*397) ^ _blockOffset.GetHashCode();
            }
        }

        public override string ToString()
        {
            return String.Format("Block {0} changed at {1}", _blockOffset, _date);
        }
    }
}