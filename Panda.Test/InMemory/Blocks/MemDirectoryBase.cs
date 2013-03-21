using System;
using System.Collections;
using System.Collections.Generic;
using Panda.Core.Blocks;

namespace Panda.Test.InMemory.Blocks
{
    public abstract class MemDirectoryBase : MemBlock, IDirectoryContinuationBlock
    {
        private readonly HashSet<DirectoryEntry> _entries = new HashSet<DirectoryEntry>();
        private readonly int _capacity;

        public MemDirectoryBase(int offset, int capacity) : base(offset)
        {
            _capacity = capacity;
        }

        public IEnumerator<DirectoryEntry> GetEnumerator()
        {
            return GuardedEnumerator(_entries);
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
                return _entries.Count;
            }
        }

        public int Capacity
        {
            get
            {
                ThrowIfDeallocated();
                return _capacity;
            }
        }

        public void DeleteEntry(DirectoryEntry entry)
        {
            ThrowIfDeallocated();
            if (!_entries.Remove(entry))
                throw new InvalidOperationException("No such directory entry in the directory block: " + entry);
        }

        public bool TryAddEntry(DirectoryEntry entry)
        {
            ThrowIfDeallocated();
            if (_entries.Count >= Capacity)
            {
                return false;
            }
            else
            {
                _entries.Add(entry);
                return true;
            }
        }
    }
}