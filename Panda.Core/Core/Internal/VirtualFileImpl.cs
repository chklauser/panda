using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Panda.Core.Blocks;

namespace Panda.Core.Internal
{
    class VirtualFileImpl : VirtualFile
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

        public override System.IO.Stream Open()
        {
            throw new NotImplementedException();
        }

        public override string Name
        {
            get { return _name; }
        }

        public override long Size
        {
            get { throw new NotImplementedException(); }
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

            // add new DirectoryEntry
            _parentDirectory.AddDirectoryEntryToCurrentDirectoryNode(newDe);
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
        }

        public override void Move(VirtualDirectory destination, string newName)
        {
            Move(destination as VirtualDirectoryImpl, newName);
        }

        public void Move(VirtualDirectoryImpl destination, string newName)
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
    }
}
