using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Panda.Core.Blocks;

namespace Panda.Core.Internal
{
    public class LastAccessCache : IBlockReferenceCache
    {
        private readonly LinkedList<IBlock> _accessOrder = new LinkedList<IBlock>();

        /// <summary>
        /// Also acts as a synchronization root.
        /// </summary>
        private readonly Dictionary<int, LinkedListNode<IBlock>> _pointerTable =
            new Dictionary<int, LinkedListNode<IBlock>>();

        public LastAccessCache(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentException("LastAccessCache capacity must be positive.", "capacity");

            Capacity = capacity;
        }

        public int Capacity { get; set; }

        public void RegisterAccess(IBlock reference)
        {
            lock (_pointerTable)
            {
                LinkedListNode<IBlock> node;
                if (_pointerTable.TryGetValue(reference.Offset, out node))
                {
                    _accessOrder.Remove(node);
                    _accessOrder.AddFirst(node);
                    return;
                }

                _insert(reference);
            }
        }

        public void EvictEarly(IBlock reference)
        {
            LinkedListNode<IBlock> node;
            if (_pointerTable.TryGetValue(reference.Offset, out node))
                _accessOrder.Remove(node);
        }

        public int EstimateSize()
        {
            return _accessOrder.Count;
        }

        private void _insert(IBlock block)
        {
            if (_accessOrder.Count > Capacity * 2)
                _truncate();
            var node = _accessOrder.AddFirst(block);
            _pointerTable.Add(block.Offset, node);
        }

        private void _truncate()
        {
            Debug.Assert(_accessOrder.Count >= Capacity,
                "Access order linked list of last access cache should be truncated but has less than $Capacity entries.");

            var buf = new IBlock[Capacity];
            var i = 0;
            foreach (var n in _accessOrder.Take(Capacity))
                buf[i++] = n;

            _accessOrder.Clear();
            _accessOrder.AddRange(buf);
            _pointerTable.Clear();
            foreach (var node in _accessOrder.ToNodeSequence())
                _pointerTable.Add(node.Value.Offset, node);
        }

        protected IEnumerable<IBlock> Contents()
        {
            lock (_pointerTable)
                foreach (var item in _accessOrder.InReverse())
                    yield return item;
        }

        protected int Count { get { return _accessOrder.Count; } }

    }
}
