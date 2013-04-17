using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Panda.Core.IO.InMemory
{
    public class InMemorySpace : IRawPersistenceSpace
    {
        private IntPtr _space;
        private int _disposedFlag;

        public InMemorySpace(uint capacity)
        {
            Capacity = capacity;
            _space = Marshal.AllocHGlobal((IntPtr) capacity);
        }

        #region Resource disposal

        ~InMemorySpace()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposedFlag, 1, 0) != 0)
                return;

            if (disposing)
            {
                Marshal.FreeHGlobal(_space);
            }
        }

        public bool IsDisposed
        {
            get { return _disposedFlag != 0; }
        }

        protected void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException("The memory mapped space cannot be used after it has been disposed.");
            }
        }

        #endregion

        public unsafe void* Pointer
        {
            get
            {
                ThrowIfDisposed();
                return _space.ToPointer();
            }
        }

        public long Capacity { get; private set; }

        public bool CanGrow
        {
            get
            {
                ThrowIfDisposed();
                return true;
            }
        }

        public bool CanShrink
        {
            get
            {
                ThrowIfDisposed();
                return true;
            }
        }

        public void Resize(long newSize)
        {
            if (newSize < Int32.MaxValue)
            {
                _space = Marshal.ReAllocHGlobal(_space, (IntPtr) newSize);
                Capacity = newSize;
            }
            else
            {
                // This isn't a technical limitation. We could allow >2GB in-memory spaces.
                // But in memory spaces **must** fit into memory and that memory will be allocated immediately on the heap, all 3TB at once :-P

                // Large memory mapped regions without persistence are less problematic, because they are not part of the heap
                // in both cases, the OS should not reserve all of the memory immediately but only when the pages are touched first (but we haven't verified this)
                throw new ArgumentOutOfRangeException("newSize",string.Format("In memory space does not support capacities beyond Int32.MaxValue ({0}).", Int32.MaxValue));
            }
        }

        public void Flush()
        {
            ThrowIfDisposed();
            // don't have to do anything
        }
    }
}