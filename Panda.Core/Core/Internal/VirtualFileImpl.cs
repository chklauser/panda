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
        private VirtualDirectoryImpl _parentDirectory;
        private string _name;


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

            if(_parentDirectory.Contains(newName))
                throw new PathAlreadyExistsException("A file or directory with the name " + newName + " already exists. Cannot rename " + _name + ".");

            // search DirectoryEntry of this directory in the parent directory
            var tuple = _parentDirectory.FindDirectoryEntry(_blockOffset);

            // create new DirectoryEntry for this directory with the new name, other stuff remains unchanged
            var newDe = new DirectoryEntry(newName, tuple.Item1.BlockOffset, tuple.Item1.Flags);

            // Have the parent add this node. Ideally, we'd put the new entry just where the old one was
            //  but as the new name can be longer than the old one, it might not necessarily fit.
            // We decided not to handle the special case where it does fit and just re-add it to the directory
            _parentDirectory.AddDirectoryEntry(newDe);

            _name = newName;
            OnPropertyChanged("Name");
            OnPropertyChanged("FullName");

            // remove old DirectoryEntry
            tuple.Item2.DeleteEntry(tuple.Item1);

            // Note how we do not notify the parent directory that its collection changed
            // as the set of entries is still the same (though the order could have changed)
        }

        /// <summary>
        /// Delete File: First delete directoryEntry in Parent. Then go trough all ContinuationBlocks
        /// and free its DataBlocks. Do the same with the FileBlock. After that free the FileBlock and 
        /// ContinuationBlocks intself.
        /// </summary>
        public override void Delete()
        {
            // delete directoryEntry of current Block
            _parentDirectory.DeleteDirectoryEntry(_blockOffset);

            // gather all ContinuationBlocks (including the offset of the head file block)
            var toDeleteBlocks = new List<BlockOffset>
                {
                    // initializes the list to contain the head file block offset
                    _blockOffset
                };
            var fileBlock = _disk.BlockManager.GetFileBlock(_blockOffset);
            var continuationBlockOffset = fileBlock.ContinuationBlockOffset;
            while (continuationBlockOffset.HasValue)
            {
                toDeleteBlocks.Add(continuationBlockOffset.Value);
                var fileContinuationBlock = _disk.BlockManager.GetFileContinuationBlock(continuationBlockOffset.Value);
                continuationBlockOffset = fileContinuationBlock.ContinuationBlockOffset;
                foreach (var offset in fileContinuationBlock)
                {
                    _disk.BlockManager.FreeBlock(offset);
                }
            }
            foreach (var offset in fileBlock)
            {
                _disk.BlockManager.FreeBlock(offset);
            }

            // delete file block and all ContinuationBlocks:
            foreach (var de in toDeleteBlocks)
            {
                _disk.BlockManager.FreeBlock(de);
            }

            // Notify disk of this deletion, this is necessary to keep the cache up to date
            _disk.OnDelete(this);
        }

        public override void Move(VirtualDirectory destination, string newName)
        {
            // The code path for rename is a bit simpler (less rewriting)
            //  we'll use rename as an optimization for "degenerate" same-directory move operations.
            if (destination == _parentDirectory)
                Rename(newName);
            else
                _move((VirtualDirectoryImpl) destination, newName);
        }

        private void _move(VirtualDirectoryImpl destination, string newName)
        {
            // check directory name
            VirtualFileSystem.CheckNodeName(newName);

            if (destination.Contains(newName))
                throw new PathAlreadyExistsException("A file or directory with the name " + newName + " in " + destination.FullName + " already exists. Cannot move " + FullName + ".");

            // search DirectoryEntry of this directory in the parent directory
            var oldDe = _parentDirectory.DeleteDirectoryEntry(_blockOffset);

            // create new DirectoryEntry for this directory with the new name, other stuff remains unchanged
            var newDe = new DirectoryEntry(newName, oldDe.BlockOffset, oldDe.Flags);

            // add new DirectoryEntry in the new destination directory
            destination.AddDirectoryEntry(newDe);

            _name = newName;
            _parentDirectory = destination;

            OnPropertyChanged("Name");
            OnPropertyChanged("ParentDirectory");
            OnPropertyChanged("FullName");
        }

        public override void Copy(VirtualDirectory destination)
        {
            _copy((VirtualDirectoryImpl) destination);
        }

        public override Task ExportAsync(string path)
        {
            return Task.Run( () =>
                {
                    using (var fs = File.Create(path))
                    {
                        using (var stream = this.Open())
                        {
                            stream.CopyTo(fs);
                        }
                    }
                }   
            );

        }

        private void _copy(VirtualDirectoryImpl destination)
        {
            destination.CreateFile(Name, Open());
        }

        public BlockOffset CacheKey
        {
            get { return _blockOffset; }
        }
    }
}
