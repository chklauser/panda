using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Panda.Core.Blocks;

namespace Panda.Core.Internal
{
    public class LastAccessCache : IBlockReferenceCache
    {
        private readonly LinkedList<Block> _accessOrder = new LinkedList<Block>();

        /// <summary>
        /// Also acts as a synchronization root.
        /// </summary>
        private readonly Dictionary<int, LinkedListNode<Block>> _pointerTable =
            new Dictionary<int, LinkedListNode<Block>>();

        public LastAccessCache(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentException("LastAccessCache capacity must be positive.", "capacity");

            Capacity = capacity;
        }

        public int Capacity { get; set; }

        public void RegisterAccess(Block reference)
        {
            lock (_pointerTable)
            {
                LinkedListNode<Block> node;
                if (_pointerTable.TryGetValue(reference.Offset, out node))
                {
                    _accessOrder.Remove(node);
                    _accessOrder.AddFirst(node);
                    return;
                }

                _insert(reference);
            }
        }

        public int EstimateSize()
        {
            return _accessOrder.Count;
        }

        private void _insert(Block block)
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

            var buf = new Block[Capacity];
            var i = 0;
            foreach (var n in _accessOrder.Take(Capacity))
                buf[i++] = n;

            _accessOrder.Clear();
            _accessOrder.AddRange(buf);
            _pointerTable.Clear();
            foreach (var node in _accessOrder.ToNodeSequence())
                _pointerTable.Add(node.Value.Offset, node);
        }

        protected IEnumerable<Block> Contents()
        {
            lock (_pointerTable)
                foreach (var item in _accessOrder.InReverse())
                    yield return item;
        }

        protected int Count { get { return _accessOrder.Count; } }

    }
}
