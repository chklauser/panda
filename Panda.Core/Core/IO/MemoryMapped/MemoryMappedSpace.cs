using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;
using System.Threading;
using JetBrains.Annotations;

namespace Panda.Core.IO.MemoryMapped
{

    /// <summary>
    /// Persistence space backed by a memory mapping. 
    /// </summary>
    /// <remarks>
    ///     <para>The memory mapping doesn't necessarily have to be a memory mapped <em>file</em>, it could also be shared memory, or a copy-on-write mapping.</para>
    ///     <para>The sub class <see cref="MemoryMappedFileSpace"/> provides specialized support for file-backed memory maps.</para>
    /// </remarks>
    public unsafe class MemoryMappedSpace : IRawPersistenceSpace
    {
        /// <summary>
        /// Ensures that we don't try to release resources more than once. Any value other than 0 means that we freed resources before.
        /// </summary>
        private int _disposedFlag;

        private MemoryMappedViewAccessor _accessor;
        private MemoryMappedFile _mappedFile;

        private byte* _pointer;

        public MemoryMappedSpace([NotNull] MemoryMappedFile mappedFile)
        {
            if (mappedFile == null)
                throw new ArgumentNullException("mappedFile");

            // Assigning the mapped file automatically creates the accessor and retrieves a pointer
            MappedFile = mappedFile;
        }

        protected virtual void ResetAccessor()
        {
            if (_accessor != null)
            {
                if(_pointer != null)
                    
                    Accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                Accessor.Dispose();
            }

            // Creates a read-write view accessor across the entire file.
            // We could support 32-bit versions of Panda by only mapping small portions of the file
            // at once. Since mapping/unmapping is not free, that would require
            // some sophisticated caching. For this project, it doesn't seem worth the effort.
            _accessor = MappedFile.CreateViewAccessor();
            _pointer = null;
            Accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _pointer);
        }

        [NotNull]
        protected MemoryMappedFile MappedFile
        {
            get { return _mappedFile; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (ReferenceEquals(_mappedFile, value))
                    return;
                _mappedFile = value;
                ResetAccessor();
            }
        }

        [NotNull]
        protected MemoryMappedViewAccessor Accessor
        {
            get { return _accessor; }
        }

        public void* Pointer
        {
            get
            {
                ThrowIfDisposed();
                return _pointer;
            }
        }

        public long Capacity
        {
            get
            {
                ThrowIfDisposed();
                return Accessor.Capacity;
            }
        }

        #region Resizing (not implemented)

        public bool CanResize
        {
            get
            {
                ThrowIfDisposed();
                return false;
            }
        }

        public void Resize(long newSize)
        {
            ThrowIfDisposed();
            throw new NotSupportedException("Memory mapped space does not support resizing.");
        }


        public void Flush()
        {
            ThrowIfDisposed();
            Accessor.Flush();
        }

        #endregion


        #region Resource disposal

        ~MemoryMappedSpace()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Interlocked.CompareExchange(ref _disposedFlag, 1, 0) != 0)
                return;

            if (disposing)
            {
                if (_pointer != null)
                {
                    // pointer must be released in a constrained execution region
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                    }
                    finally
                    {
                        Accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                    }
                }
            }
            MappedFile.Dispose();
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

    }
}