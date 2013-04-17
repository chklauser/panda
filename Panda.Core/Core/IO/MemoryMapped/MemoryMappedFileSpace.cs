using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Panda.Core.Internal;

namespace Panda.Core.IO.MemoryMapped
{
    public class MemoryMappedFileSpace : MemoryMappedSpace
    {
        [NotNull]
        private readonly String _path;

        public MemoryMappedFileSpace([NotNull] string path)
            : base(_mapExistingFile(path))
        {
            _path = path;
        }

        protected MemoryMappedFileSpace([NotNull] MemoryMappedFile mappedFile, [NotNull] string path) : base(mappedFile)
        {
            _path = path;
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
            using (var file = new FileStream(path,FileMode.CreateNew,FileAccess.Write))
            {
                var writer = new BinaryWriter(file, Encoding.UTF8, true);
                writer.Write(blockCapacity);
                writer.Write(blockSize);

                var clCapa = (uint)Math.Min(UInt32.MaxValue, capacity);
                var fullPath = Path.GetFullPath(path);
                if (clCapa > 64*1024 - blockCapacity && SparseFile.VolumeSupportsSparseFiles(Path.GetPathRoot(fullPath)))
                {
                    SparseFile.Convert(file.SafeFileHandle);
                    file.Seek(capacity,SeekOrigin.Begin);
                    SparseFile.SetSparseRange(file.SafeFileHandle, blockSize,clCapa-blockSize);
                }
            }
            return new MemoryMappedFileSpace(MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, capacity, MemoryMappedFileAccess.ReadWrite),path);
        }

        public override unsafe void Resize(long newSize)
        {
            // This method might use path if it is implemented
            _path.Ignore();
            throw new NotImplementedException("MemoryMappedFileSpace.Resize is not implemented.");
        }
    }
}
