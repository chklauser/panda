using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Win32.SafeHandles;
using Panda.Core.Internal;

namespace Panda.Core.IO.MemoryMapped
{
    public class MemoryMappedFileSpace : MemoryMappedSpace
    {
        private uint _nonSparseSize;

        public MemoryMappedFileSpace([NotNull] string path)
            : base(_mapExistingFile(path))
        {
        }

        protected MemoryMappedFileSpace([NotNull] MemoryMappedFile mappedFile, uint nonSparseSize) : base(mappedFile)
        {
            _nonSparseSize = nonSparseSize;
        }

        private static MemoryMappedFile _mapExistingFile(string path)
        {
            long capacity;
            using (var file = new FileStream(path,FileMode.Open))
            {
                var reader = new BinaryReader(file, Encoding.UTF8, true);
                long numBlocks = reader.ReadUInt32();
                long blockSize = reader.ReadUInt32();
                capacity = numBlocks*blockSize;
            }
            return MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, capacity, MemoryMappedFileAccess.ReadWrite);
        }

        /// <summary>
        /// Initializes and opens a new file, setting the block capacity and block size.
        /// </summary>
        /// <param name="path">The path of the file to create.</param>
        /// <param name="blockSize">Size of an invidual block in number of bytes.</param>
        /// <param name="blockCapacity">Number of blocks the space can hold.</param>
        /// <returns>A memory mapped file space for the newly created file.</returns>
        /// <remarks><para>Memory mapped file spaces use the block size and block capacity fields when mapping a file.</para>
        /// <para>This method doesn't fully initialize a disk file, only the block capacity and block size fields.</para></remarks>
        public static MemoryMappedFileSpace CreateNew(string path, uint blockSize, uint blockCapacity)
        {
            var capacity = blockSize * (long)blockCapacity;
            var clCapa = (uint)Math.Min(UInt32.MaxValue, capacity);
            using (var file = new FileStream(path,FileMode.CreateNew,FileAccess.Write))
            {
                var writer = new BinaryWriter(file, Encoding.UTF8, true);
                writer.Write(blockCapacity);
                writer.Write(blockSize);

                if (clCapa > 64*1024 - blockCapacity && SparseFile.VolumeSupportsSparseFiles(Path.GetPathRoot(Path.GetFullPath(path))))
                {
                    SparseFile.Convert(file.SafeFileHandle);
                    file.Seek(capacity,SeekOrigin.Begin);
                    SparseFile.SetSparseRange(file.SafeFileHandle, blockSize,clCapa-blockSize);
                }
            }
            return new MemoryMappedFileSpace(MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, capacity, MemoryMappedFileAccess.ReadWrite),clCapa);
        }

        public override bool CanGrow
        {
            get { return true; }
        }

        public override bool CanShrink
        {
            get { return true; }
        }

        public override void Resize(long newSize)
        {
            var cappedNewSize = (uint)Math.Min(UInt32.MaxValue, newSize);

            if (cappedNewSize > _nonSparseSize)
            {
                _nonSparseSize = cappedNewSize;
            }
            else if(_nonSparseSize - newSize > 64*1024)
            {
                // A sufficient amount of free space has accumulated beyond the break to 
                // replace with a sparse region.
                var wrappedFileHandle = new SafeFileHandle(MappedFile.SafeMemoryMappedFileHandle.DangerousGetHandle(), false);
                SparseFile.SetSparseRange(wrappedFileHandle,cappedNewSize,_nonSparseSize-cappedNewSize);
                _nonSparseSize = cappedNewSize;
            }
            else
            {
                // No change in size or not shrunk sufficiently, ignore this resize
            }
        }
    }
}
