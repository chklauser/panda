using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Panda.Core.Internal
{
    public class LastAccessCache<TKey,TValue> : IReferenceCache<TValue> where TValue : class, ICacheKeyed<TKey>
    {
        private readonly LinkedList<TValue> _accessOrder = new LinkedList<TValue>();

        /// <summary>
        /// Also acts as a synchronization root.
        /// </summary>
        private readonly Dictionary<TKey, LinkedListNode<TValue>> _pointerTable =
            new Dictionary<TKey, LinkedListNode<TValue>>();

        public LastAccessCache(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentException("LastAccessCache capacity must be positive.", "capacity");

            Capacity = capacity;
        }

        public int Capacity { get; set; }

        public void RegisterAccess(TValue reference)
        {
            lock (_pointerTable)
            {
                LinkedListNode<TValue> node;
                if (_pointerTable.TryGetValue(reference.CacheKey, out node))
                {
                    _accessOrder.Remove(node);
                    _accessOrder.AddFirst(node);
                    return;
                }

                _insert(reference);
            }
        }

        public void EvictEarly(TValue reference)
        {
            LinkedListNode<TValue> node;
            if (_pointerTable.TryGetValue(reference.CacheKey, out node))
            {
                _accessOrder.Remove(node);
                _pointerTable.Remove(reference.CacheKey);
            }
        }

        public int EstimateSize()
        {
            return _accessOrder.Count;
        }

        private void _insert(TValue item)
        {
            if (_accessOrder.Count > Capacity * 2)
                _truncate();
            var node = _accessOrder.AddFirst(item);
            _pointerTable.Add(item.CacheKey, node);
        }

        private void _truncate()
        {
            Debug.Assert(_accessOrder.Count >= Capacity,
                "Access order linked list of last access cache should be truncated but has less than $Capacity entries.");

            var buf = new TValue[Capacity];
            var i = 0;
            foreach (var n in _accessOrder.Take(Capacity))
                buf[i++] = n;

            _pointerTable.Clear();
            _accessOrder.Clear();
            _accessOrder.AddRange(buf);
            _pointerTable.Clear();
            foreach (var node in _accessOrder.ToNodeSequence())
                _pointerTable.Add(node.Value.CacheKey, node);
        }

        protected IEnumerable<TValue> Contents()
        {
            lock (_pointerTable)
                foreach (var item in _accessOrder.InReverse())
                    yield return item;
        }

        protected int Count { get { return _accessOrder.Count; } }

    }
}
