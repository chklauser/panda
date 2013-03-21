using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Panda.Core.Blocks;

namespace Panda.Test.InMemory.Blocks
{
    public class MemOffsetList : MemBlock, IEmptyListBlock, IFileContinuationBlock
    {
        [NotNull]
        private readonly List<int> _offsets = new List<int>();
        private readonly int _listCapacity;
        private int _totalFreeBlockCount;

        public MemOffsetList(int offset, int listCapacity) : base(offset)
        {
            _listCapacity = listCapacity;
        }

        public IEnumerator<int> GetEnumerator()
        {
            return GuardedEnumerator(_offsets);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count
        {
            get
            {
                ThrowIfDeallocated(); 
                return Offsets.Count;
            }
        }

        public List<int> Offsets
        {
            get
            {
                ThrowIfDeallocated();
                return _offsets;
            }
        }

        public int TotalFreeBlockCount
        {
            get
            {
                ThrowIfDeallocated();
                return _totalFreeBlockCount;
            }
            set
            {
                ThrowIfDeallocated();
                _totalFreeBlockCount = value;
            }
        }

        public int[] Remove(int count)
        {
            ThrowIfDeallocated();
            var rs = new int[count];
            _offsets.CopyTo(_offsets.Count - count, rs, 0, count);
            _offsets.RemoveRange(_offsets.Count - count, count);
            return rs;
        }

        public void Append(int[] freeBlockOffsets)
        {
            ThrowIfDeallocated();
            if (_offsets.Count + freeBlockOffsets.Length > ListCapacity)
            {
                throw new ArgumentOutOfRangeException("freeBlockOffsets", "Not enough free space in offset list.");
            }
            _offsets.AddRange(freeBlockOffsets);
        }

        public int ListCapacity
        {
            get
            {
                ThrowIfDeallocated();
                return _listCapacity;
            }
        }

        public void ReplaceOffsets(int[] offsets)
        {
            ThrowIfDeallocated();
            _offsets.Clear();
            Append(offsets);
        }
    }
}