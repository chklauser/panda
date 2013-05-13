using System;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;
using Panda.Core.Blocks;

namespace Panda.Core.Internal
{

    /// <summary>
    /// This trait is added on to virtual disk implementation instances and ensures that for every (logical) directory or node, there
    /// is exactly one or zero Virtual*Impl instances that handle that file system entity.
    /// </summary>
    internal abstract class SingleInstanceVirtualDiskImpl : VirtualDiskImpl
    {
        protected SingleInstanceVirtualDiskImpl([NotNull] IBlockManager blockManager, [NotNull] ILockingPolicy lockingPolicy) : base(blockManager, lockingPolicy)
        {
        }

                [NotNull]
        protected abstract IReferenceCache<ICacheKeyed<BlockOffset>> ReferenceCache { get; }

        #region Default implementation

        [NotNull]
        public static SingleInstanceVirtualDiskImpl Create([NotNull] IBlockManager blockManager, [NotNull] ILockingPolicy lockingPolicy)
        {
            return new Default(blockManager, lockingPolicy);
        }

        private class Default : SingleInstanceVirtualDiskImpl
        {
            [NotNull]
            private readonly IReferenceCache<ICacheKeyed<BlockOffset>> _referenceCache = new LastAccessCache<BlockOffset, ICacheKeyed<BlockOffset>>(512);

            public Default([NotNull] IBlockManager blockManager, [NotNull] ILockingPolicy lockingPolicy)
                : base(blockManager, lockingPolicy)
            {
            }

            protected override IReferenceCache<ICacheKeyed<BlockOffset>> ReferenceCache
            {
                get { return _referenceCache; }
            }
        }

        #endregion

        private readonly Dictionary<BlockOffset, WeakReference<ICacheKeyed<BlockOffset>>> _existingNodes =
            new Dictionary<BlockOffset, WeakReference<ICacheKeyed<BlockOffset>>>();

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        protected SemaphoreSlim Lock
        {
            get { return _lock; }
        }

        private const int GcInterval = 2048;

        private void _trackNewNode(ICacheKeyed<BlockOffset> node)
        {
            // assume exclusive access
            ReferenceCache.RegisterAccess(node);
            ICacheKeyed<BlockOffset> existing;
            WeakReference<ICacheKeyed<BlockOffset>> weakExisting;
            if (_existingNodes.TryGetValue(node.CacheKey, out weakExisting) && weakExisting.TryGetTarget(out existing))
            {
                if (!ReferenceEquals(existing, node))
                {
                    throw new InvalidOperationException("Two node instances for the same offset detected.");
                }
                else
                {
                    // already added
                }
            }
            else
            {
                _existingNodes[node.CacheKey] = new WeakReference<ICacheKeyed<BlockOffset>>(node);
            }
        }

        private uint _gcCounter;

        private void _handleGc()
        {
            // Assume exclusive access
            if (_gcCounter++ > GcInterval)
            {
                _gcCounter = 0;
                _collectGarbage();
            }
        }

        private void _collectGarbage()
        {
            // assume exclusive access
            var gced = new List<BlockOffset>();
            foreach (var weakRefEntry in _existingNodes)
            {
                ICacheKeyed<BlockOffset> dummy;
                if (!weakRefEntry.Value.TryGetTarget(out dummy))
                {
                    gced.Add(weakRefEntry.Key);
                }
            }

            foreach (var offset in gced)
            {
                _existingNodes.Remove(offset);
            }
        }

        [ContractAnnotation("=>true,node:notnull; =>false,node:null")]
        private bool _tryGetExisting<T>(BlockOffset offset, out T node) where T : class, ICacheKeyed<BlockOffset>
        {
            // assume exclusive access
            WeakReference<ICacheKeyed<BlockOffset>> weakNode;
            ICacheKeyed<BlockOffset> anyNode;
            if (_existingNodes.TryGetValue(offset, out weakNode)
                && weakNode.TryGetTarget(out anyNode))
            {
                node = (T)anyNode;
                return true;
            }
            else
            {
                node = null;
                return false;
            }
        }

        protected internal override void OnDelete(ICacheKeyed<BlockOffset> node)
        {
            if (node == null)
                throw new ArgumentNullException("node");
            
            base.OnDelete(node);
            _lock.Wait();
            try
            {
                ReferenceCache.EvictEarly(node);
                _existingNodes.Remove(node.CacheKey);
                _handleGc();
            }
            finally
            {
                _lock.Release();
            }
        }

        public override void ReceiveChanges(BlockOffset blockOffset, byte[] data)
        {
            base.ReceiveChanges(blockOffset, data);

            // This override makes sure that any nodes that were associated with that particular block offset
            // are flushed from the cache. They might after all have changed their representation
            _lock.Wait();
            try
            {
                WeakReference<ICacheKeyed<BlockOffset>> weakNode;
                ICacheKeyed<BlockOffset> node;
                if (_existingNodes.TryGetValue(blockOffset, out weakNode) && weakNode.TryGetTarget(out node))
                {
                    ReferenceCache.EvictEarly(node);
                    _existingNodes.Remove(blockOffset);
                    _handleGc();
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        internal override VirtualDirectoryImpl GetDirectory(VirtualDirectoryImpl parentDirectory, DirectoryEntry de)
        {
            _lock.Wait();
            try
            {
                _handleGc();
                VirtualDirectoryImpl node;
                if (!_tryGetExisting(de.BlockOffset, out node))
                {
                    node = base.GetDirectory(parentDirectory, de);
                    _trackNewNode(node);
                }
                
                return node;
            }
            finally
            {
                _lock.Release();
            }
        }

        internal override VirtualFileImpl GetFile(VirtualDirectoryImpl parentDirectory, DirectoryEntry de)
        {
            _lock.Wait();
            try
            {
                _handleGc();
                VirtualFileImpl node;
                if (!_tryGetExisting(de.BlockOffset, out node))
                {
                    node = base.GetFile(parentDirectory, de);
                    _trackNewNode(node);
                }

                return node;
            }
            finally
            {
                _lock.Release();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _lock.Dispose();
            }
        }
    }
}