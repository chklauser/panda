using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Panda.Core.Blocks;

namespace Panda.Core.Internal
{
    class VirtualDirectoryImpl : VirtualDirectory
    {
        protected readonly VirtualDiskImpl _disk;
        protected readonly BlockOffset _blockOffset;
        private readonly string _name;
        private readonly VirtualDirectoryImpl _parentDirectory;

        public VirtualDirectoryImpl(VirtualDiskImpl disk, BlockOffset blockOffset, VirtualDirectoryImpl parentDirectory, string name)
        {
            _disk = disk;
            _blockOffset = blockOffset;
            _parentDirectory = parentDirectory;
            _name = name;
        }

        public override VirtualNode Navigate(string path)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            
            // check if absolute or relative path given
            if (PathUtil.isAbsolutePath(path))
            {
                // absolute path given, call Navigate on root DirectoryNode with the same path, but without /
                return _disk.Root.Navigate(path.Substring(1, path.Length));
            }
            else
            {
                // relative path given, parse path and call Navigate(Array<string>)
                return Navigate(Panda.Core.PathUtil.parsePath(path));
            }
        }

        public override VirtualNode Navigate(string[] path)
        {
            return Navigate(new Queue<string>(path));
        }

        /// <summary>
        /// Overloads Navigate with <see cref="Queue{String}"/>, which is a cooler data structure for this use.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public VirtualNode Navigate(Queue<string> path)
        {
            // is path empty?
            if (path.Count == 0)
            {
                throw new IllegalPathException("Empty path.");
            }
            else
            {
                // path is not empty, so take the first element of it
                string nodeName = path.Dequeue();
                VirtualNode retNode;

                // try to find the given node by name
                if (TryGetNode(nodeName, out retNode))
                {
                    // node found
                    // try to interpret the found node as directory
                    VirtualDirectoryImpl childDirectory = retNode as VirtualDirectoryImpl;
                    if (childDirectory != null)
                    {
                        // node is a directory
                        return childDirectory.Navigate(path);
                    }
                    else
                    {
                        // node must be a file
                        VirtualFileImpl childFile = retNode as VirtualFileImpl;
                        if (childFile != null)
                        {
                            // node is a file, so the path must now be empty, otherwise the path is invalid
                            if (path.Count == 0)
                            {
                                return childFile;
                            }
                            else
                            {
                                throw new IllegalPathException("File node found, but not at end of path.");
                            }
                        }
                    }
                    // node is neither interpretable as directory or file
                    throw new PandaException("Node is neither interpretable as directory or file.");
                }
                else
                {
                    throw new PathNotFoundException();
                }
            }
        }

        public override IEnumerator<VirtualNode> GetEnumerator()
        {
            // get first offset (of current DirectoryBlock)
            IDirectoryContinuationBlock currentDirectoryBlock = _disk.BlockManager.GetDirectoryBlock(_blockOffset);

            // iterate over current DirectoryBlock and return VirtualNodes => DirectoryEntries
            foreach (DirectoryEntry de in currentDirectoryBlock)
            {
                // DirectoryEntry => tells me if file or directory
                // => call getdirectoryblock / getfileblock
                // return this node
                if (de.IsDirectory)
                {
                    yield return new VirtualDirectoryImpl(_disk, de.BlockOffset, this, de.Name);
                }
                else
                {
                    yield return new VirtualFileImpl(_disk, de.BlockOffset, this, de.Name);
                }
            }

            // do while we have continuation blocks iterate over DirectoryContinuationBlock(s) => DirectoryEntries
            while (currentDirectoryBlock.ContinuationBlockOffset != null)
            {
                // .Value is needed because ContinuationBlockOffset is nullable
                currentDirectoryBlock = _disk.BlockManager.GetDirectoryContinuationBlock(currentDirectoryBlock.ContinuationBlockOffset.Value);
                foreach (DirectoryEntry de in currentDirectoryBlock)
                {
                    // DirectoryEntry tells me if file or directory
                    // return the corresponding VirtualNode
                    if (de.IsDirectory)
                    {
                        yield return new VirtualDirectoryImpl(_disk, de.BlockOffset, this, de.Name);
                    }
                    else
                    {
                        yield return new VirtualFileImpl(_disk, de.BlockOffset, this, de.Name);
                    }
                }
            } 
        }

        /// <summary>
        /// Finds DirectoryEntry by BlockOffset on the current VirtualDirectory instance.
        /// </summary>
        /// <param name="blockOffset">BlockOffset of VirtualNode to find in the DirectoryEntries.</param>
        /// <returns>Tuple of DirectoryEntry and DirectoryContinuationBlock.</returns>
        public Tuple<DirectoryEntry, IDirectoryContinuationBlock> FindDirectoryEntry(BlockOffset blockOffset)
        {
            // first currentDirectoryBlock
            IDirectoryContinuationBlock currentDirectoryBlock = _disk.BlockManager.GetDirectoryBlock(_blockOffset);
            foreach (var de in currentDirectoryBlock)
            {
                if (blockOffset == _blockOffset)
                {
                    return Tuple.Create(de, currentDirectoryBlock);
                }
            }

            // search in ContinuationBlocks
            while (currentDirectoryBlock.ContinuationBlockOffset != null)
            {
                // .Value is needed because ContinuationBlock is nullable
                currentDirectoryBlock = _disk.BlockManager.GetDirectoryContinuationBlock(currentDirectoryBlock.ContinuationBlockOffset.Value);
                foreach (DirectoryEntry de in currentDirectoryBlock)
                {
                    if (blockOffset == _blockOffset)
                    {
                        return Tuple.Create(de, currentDirectoryBlock);
                    }
                }
            }

            // DirectoryEntry not found!
            throw new PandaException("DirectoryEntry not found!");
        }

        public override int Count
        {
            get
            {
                return this.Count<VirtualNode>();
            }
        }

        public override bool Contains(string name)
        {
            VirtualNode vn;
            return TryGetNode(name, out vn);
        }

        public override bool TryGetNode(string name, out VirtualNode value)
        {
            foreach (VirtualNode vn in this)
            {
                if (vn.Name == name)
                {
                    value = vn;
                    return true;
                }
            }
            value = null;
            return false;
        }

        public override Task<VirtualNode> ImportAsync(string path)
        {
            throw new NotImplementedException();
        }

        public override Task ExportAsync(string path)
        {
            throw new NotImplementedException();
        }

        public override VirtualDirectory CreateDirectory(string name)
        {
            // create new DirectoryBlock
            IDirectoryBlock db = _disk.BlockManager.AllocateDirectoryBlock();

            // create new DirectoryEntry
            DirectoryEntry de = new DirectoryEntry(name, db.Offset, DirectoryEntryFlags.Directory);

            // add DirectoryEntry referencing this new Block to this DirectoryBlock or a DirectoryContinuationBlock of it
            AddDirectoryEntryToCurrentDirectoryNode(de);

            return new VirtualDirectoryImpl(_disk, db.Offset, this, name);
        }

        /// <summary>
        /// Adds a DirectoryEntry to this DirectoryBlock or a DirectoryContinuationBlock
        /// </summary>
        /// <param name="de">DirectoryEntry to add</param>
        public void AddDirectoryEntryToCurrentDirectoryNode(DirectoryEntry de)
        {
            bool nodeAdded = false;

            // try to add the DirectoryEntry to this DirectoryBlock or find a DirectoryContinuationBlock until its added
            IDirectoryContinuationBlock currentBlock = _disk.BlockManager.GetDirectoryBlock(_blockOffset);

            // try to add the DirectoryEntry to this DirectoryBlock
            if (currentBlock.TryAddEntry(de))
            {
                nodeAdded = true;
            }

            // if node wasn't added, try to add the DirectoryEntry to any DirectoryContinuationBlock of this DirectoryBlock
            if (!nodeAdded)
            {
                while (currentBlock.ContinuationBlockOffset.HasValue)
                {
                    currentBlock = _disk.BlockManager.GetDirectoryContinuationBlock(currentBlock.ContinuationBlockOffset.Value);
                    if (currentBlock.TryAddEntry(de))
                    {
                        nodeAdded = true;
                        break;
                    }
                }
            }

            // check if node was added, if not, create a new ContinuationBlock and add the DirectoryEntry there
            if (!nodeAdded)
            {
                // create a new DirectoryContinuationBlock
                IDirectoryContinuationBlock newBlock = _disk.BlockManager.AllocateDirectoryContinuationBlock();
                // link it to the last ContinuationBlock seen
                currentBlock.ContinuationBlockOffset = newBlock.Offset;
                // add DirectoryEntry to it
                if (!newBlock.TryAddEntry(de))
                {
                    throw new PandaException("DirectoryEntry could not be added to a fresh, new DirectoryContinuationBlock.");
                }
            }
        }

        public override Task<VirtualFile> CreateFileAsync(string name, System.IO.Stream dataSource)
        {
            // check if the stream is readable
            if (!dataSource.CanRead)
            {
                throw new PandaException("Stream not readable.");
            }
            return Task.Run(
                () =>
                    {
                        // check file name
                        VirtualFileSystem.CheckNodeName(name);

                        // create new FileBlock
                        var fb = _disk.BlockManager.AllocateFileBlock();

                        // create a new DirectoryEntry and add address to FileBlock to it
                        var de = new DirectoryEntry(name, fb.Offset, DirectoryEntryFlags.None);

                        // create buffer with size of a data block
                        var buffer = new byte[_disk.BlockManager.DataBlockSize];

                        // keep track of file size
                        long fileSize = 0;
                        
                        // and of how many bytes read
                        int bytesRead = 0;

                        // and use a list of block offsets to data blocks
                        var dataBlocks = new List<BlockOffset>();

                        // and a current dataBlock offset
                        BlockOffset dataBlockOffset;

                        // read entire data blocks from stream
                        while ((bytesRead = dataSource.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            // sum up file size
                            fileSize += bytesRead;

                            // create new data block
                            dataBlockOffset = _disk.BlockManager.AllocateDataBlock();

                            // write data into data block
                            _disk.BlockManager.WriteDataBlock(dataBlockOffset, buffer);

                            // add address of current data block to array
                            dataBlocks.Add(dataBlockOffset);
                        }

                        // less than one data block read => finished stream reading

                            // sum uf file size
                            fileSize += bytesRead;
                            // create new data block
                            dataBlockOffset = _disk.BlockManager.AllocateDataBlock();
                            // write data into data block
                            _disk.BlockManager.WriteDataBlock(dataBlockOffset, buffer);
                            // add address of current data block to array
                            dataBlocks.Add(dataBlockOffset);


                        // write all the data offset blocks into the files blocks

                            // keep track of the current fileblock
                            var currentFileBlock = fb as IFileContinuationBlock;

                            // and of how many data block offsets already written
                            int numDataBlockOffsetsWritten = 0;

                            // add each data block offset to the file blocks
                            foreach (BlockOffset offset in dataBlocks)
                            {
                                // does the file block have remaining capacity to add the data block offset to it?
                                if (currentFileBlock.Count >= currentFileBlock.ListCapacity)
                                {
                                    // if not
                                    // create e new file block
                                    var newFileBlock = _disk.BlockManager.AllocateFileContinuationBlock();

                                    // and add its offset to the continuation block offset of the other 
                                    currentFileBlock.ContinuationBlockOffset = newFileBlock.Offset;

                                    // and set the new file block as current file block
                                    currentFileBlock = newFileBlock;
                                }

                                // TODO: ToArray's could be cached
                                // add as many block offsets to the current file block as possible
                                Array.Copy(dataBlocks.ToArray(),
                                    (long) numDataBlockOffsetsWritten - 1,
                                    currentFileBlock.ToArray(),
                                    (long) currentFileBlock.Count - 1,
                                    (long) currentFileBlock.ListCapacity - currentFileBlock.Count);

                                numDataBlockOffsetsWritten += currentFileBlock.ListCapacity - currentFileBlock.Count;
                            }

                        // add DirectoryEntry to this DirectoryBlock or a DirectoryContinuationBlock of it
                        AddDirectoryEntryToCurrentDirectoryNode(de);

                        // don't close the stream

                        // write file size
                        fb.Size = fileSize;

                        // return VirtualFile
                        return (VirtualFile) new VirtualFileImpl(_disk, fb.Offset, this, name);
                    }
                );
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
            // check directory name
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

        public override void Delete()
        {
            throw new NotImplementedException();
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
