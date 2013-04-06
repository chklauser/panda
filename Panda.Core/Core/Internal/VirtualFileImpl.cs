using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Panda.Core.Blocks;

namespace Panda.Core.Internal
{
    class VirtualFileImpl : VirtualFile, ICacheKeyed<BlockOffset>
    {
        private readonly VirtualDiskImpl _disk;
        private readonly BlockOffset _blockOffset;
        private readonly VirtualDirectoryImpl _parentDirectory;
        private readonly string _name;


        public VirtualFileImpl(VirtualDiskImpl disk, BlockOffset blockOffset, VirtualDirectoryImpl parentDirectory, string name)
        {
            _disk = disk;
            _blockOffset = blockOffset;
            _parentDirectory = parentDirectory;
            _name = name;
        }

        public override Stream Open()
        {
            return new VirtualFileOpenStream(_disk, _disk.BlockManager.GetFileBlock(_blockOffset));
        }

        public override string Name
        {
            get { return _name; }
        }

        public override long Size
        {
            get { return _disk.BlockManager.GetFileBlock(_blockOffset).Size; }
        }

        public override bool IsRoot
        {
            get { return false; }
        }

        public override VirtualDirectory ParentDirectory
        {
            get { return _parentDirectory; }
        }

        public override void Rename(string newName)
        {
            // check file name
            VirtualFileSystem.CheckNodeName(newName);

            // search DirectoryEntry of this directory in the parent directory
            var tuple = _parentDirectory.FindDirectoryEntry(_blockOffset);

            // create new DirectoryEntry for this directory with the new name, other stuff remains unchanged
            var newDe = new DirectoryEntry(newName, tuple.Item1.BlockOffset, tuple.Item1.Flags);

            // remove old DirectoryEntry
            tuple.Item2.DeleteEntry(tuple.Item1);

            // add new DirectoryEntry (should have place in the same file block!)
            if (!tuple.Item2.TryAddEntry(newDe))
            {
                throw new PandaException("Could not add new DirectoryEntry");
            }
        }

        /// <summary>
        /// Delete File: First delete directoryEntry in Parent. Then go trough all ContinuationBlocks
        /// and free its DataBlocks. Do the same with the FileBlock. After that free the FileBlock and 
        /// ContinuationBlocks intself.
        /// </summary>
        public override void Delete()
        {
            // delete directoryEntry of current Block
            var tupel = _parentDirectory.FindDirectoryEntry(_blockOffset);
            tupel.Item2.DeleteEntry(tupel.Item1);

            // gather all ContinuationBlocks:
            var toDeleteBlocks = new List<BlockOffset> {_blockOffset};
            var fileBlock = _disk.BlockManager.GetFileBlock(_blockOffset);
            var continuationBlock = fileBlock.ContinuationBlockOffset;
            while (continuationBlock.HasValue)
            {
                toDeleteBlocks.Add(continuationBlock.Value);
                var fileContinuationBlock = _disk.BlockManager.GetFileContinuationBlock(continuationBlock.Value);
                continuationBlock = fileContinuationBlock.ContinuationBlockOffset;
                foreach (var offset in fileContinuationBlock)
                {
                    _disk.BlockManager.FreeBlock(offset);
                }
            }
            foreach (var offset in fileBlock)
            {
                _disk.BlockManager.FreeBlock(offset);
            }

            // delete block and all ContinuationBlocks:
            foreach (var de in toDeleteBlocks)
            {
                _disk.BlockManager.FreeBlock(de);
            }

            // Notify disk of this deletion, this is necessary to keep the cache up to date
            _disk.OnDelete(this);
        }

        public override void Move(VirtualDirectory destination, string newName)
        {
            _move((VirtualDirectoryImpl) destination, newName);
        }

        private void _move(VirtualDirectoryImpl destination, string newName)
        {
            // check directory name
            VirtualFileSystem.CheckNodeName(newName);

            // search DirectoryEntry of this directory in the parent directory
            var tuple = _parentDirectory.FindDirectoryEntry(_blockOffset);

            // create new DirectoryEntry for this directory with the new name, other stuff remains unchanged
            var newDe = new DirectoryEntry(newName, tuple.Item1.BlockOffset, tuple.Item1.Flags);

            // remove old DirectoryEntry
            tuple.Item2.DeleteEntry(tuple.Item1);

            // add new DirectoryEntry in the new destination directory
            destination.AddDirectoryEntryToCurrentDirectoryNode(newDe);
        }

        public override void Copy(VirtualDirectory destination)
        {
            _copy((VirtualDirectoryImpl) destination);
        }

        public override Task ExportAsync(string path)
        {
            return Task.Run( () =>
                {
                    var fs = File.Create(path);
                    using (var stream = this.Open())
                    {
                        stream.CopyTo(fs);
                    }
                }   
            );

        }

        private void _copy(VirtualDirectoryImpl destination)
        {
            destination.CreateFile(this.Name, this.Open());
        }

        public BlockOffset CacheKey
        {
            get { return _blockOffset; }
        }
    }
}
