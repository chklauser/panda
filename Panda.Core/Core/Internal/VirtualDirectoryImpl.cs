using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Panda.Core.Blocks;

namespace Panda.Core.Internal
{
    class VirtualDirectoryImpl : VirtualDirectory, ICacheKeyed<BlockOffset>
    {
        private readonly VirtualDiskImpl _disk;
        private readonly BlockOffset _blockOffset;
        private string _name;
        private VirtualDirectoryImpl _parentDirectory;

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
                return _disk.Root.Navigate(path.Substring(1, path.Length-1));
            }
            else
            {
                // relative path given, parse path and call Navigate(Array<string>)
                return Navigate(PathUtil.parsePath(path));
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
                return this;
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
                    var childDirectory = retNode as VirtualDirectoryImpl;
                    if (childDirectory != null)
                    {
                        // node is a directory
                        return childDirectory.Navigate(path);
                    }
                    else
                    {
                        // node must be a file
                        var childFile = retNode as VirtualFileImpl;
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
                    yield return _disk.GetDirectory(this, de);
                }
                else
                {
                    yield return _disk.GetFile(this, de);
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
                        yield return _disk.GetDirectory(this,de);
                    }
                    else
                    {
                        yield return _disk.GetFile(this, de);
                    }
                }
            } 
        }

        /// <summary>
        /// Finds DirectoryEntry by BlockOffset on the current VirtualDirectory instance.
        /// </summary>
        /// <param name="blockOffset">BlockOffset of VirtualNode to find in the DirectoryEntries.</param>
        /// <returns>Tuple of DirectoryEntry and DirectoryContinuationBlock.</returns>
        public Tuple<DirectoryEntry, IDirectoryContinuationBlock, IDirectoryContinuationBlock, int> FindDirectoryEntry(BlockOffset blockOffset)
        {
            // first currentDirectoryBlock
            IDirectoryContinuationBlock currentDirectoryBlock = _disk.BlockManager.GetDirectoryBlock(_blockOffset);
            var entryIndex = 0;
            foreach (var de in currentDirectoryBlock)
            {
                if (blockOffset == de.BlockOffset)
                {
                    return Tuple.Create(de, currentDirectoryBlock,(IDirectoryContinuationBlock) null,entryIndex);
                }
                entryIndex++;
            }

            IDirectoryContinuationBlock previousDirectoryBlock = currentDirectoryBlock;
            // search in ContinuationBlocks
            while (currentDirectoryBlock.ContinuationBlockOffset != null)
            {
                // .Value is needed because ContinuationBlockOffset is nullable
                currentDirectoryBlock = _disk.BlockManager.GetDirectoryContinuationBlock(currentDirectoryBlock.ContinuationBlockOffset.Value);
                foreach (DirectoryEntry de in currentDirectoryBlock)
                {
                    if (blockOffset == de.BlockOffset)
                    {
                        return Tuple.Create(de, currentDirectoryBlock, previousDirectoryBlock,entryIndex);
                    }
                }

                previousDirectoryBlock = currentDirectoryBlock;
                entryIndex++;
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
            return Task.Run(
                () =>
                    {
                        var node = _import(path);
                        return node;
                    });
        }

        private VirtualNode _import([NotNull] string path)
        {
            if (File.Exists(path))
            {
                // 'using' is shortcut sucht that we don't have to close the FileStream in the end. Done automatically due to using.
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    // ReSharper disable AssignNullToNotNullAttribute
                    return CreateFile(Path.GetFileName(path), fs);
                    // ReSharper restore AssignNullToNotNullAttribute
                }
            }
            else if (Directory.Exists(path))
            {
                // ReSharper disable AssignNullToNotNullAttribute
                var directory = (VirtualDirectoryImpl) CreateDirectory(Path.GetFileName(path));
                // ReSharper restore AssignNullToNotNullAttribute
                var content = Directory.EnumerateFileSystemEntries(path);
                foreach (var current in content)
                {
                    directory._import(current);
                }
                return directory;
            }
            else
            {
                // FileSystemEntrie not found!
                throw new PandaException("FileSystemEntrie not found!");
            }
        }

        public override Task ExportAsync(string path)
        {
            return Task.Run(() => _export(path));
        }

        private void _export(string path)
        {
            Directory.CreateDirectory(path);
            foreach (var de in this)
            {
                path = Path.Combine(path, de.Name);
                de.Export(path);
            }
        }

        public override VirtualDirectory CreateDirectory(string name)
        {
            if (Contains(name))
                throw new PathAlreadyExistsException("A file or directory with the name " + name + " already exists. Cannot create directory " + name + ".");

            // create new DirectoryBlock
            IDirectoryBlock db = _disk.BlockManager.AllocateDirectoryBlock();

            // create new DirectoryEntry
            DirectoryEntry de = new DirectoryEntry(name, db.Offset, DirectoryEntryFlags.Directory);

            // add DirectoryEntry referencing this new Block to this DirectoryBlock or a DirectoryContinuationBlock of it
            AddDirectoryEntry(de);

            var dir = _disk.GetDirectory(this, de);

            _disk.BlockManager.Flush();

            // No need to NotifyCollectionChanged here, AddDirectoryEntry already did that

            return dir;
        }

        /// <summary>
        /// Adds a DirectoryEntry to this DirectoryBlock or a DirectoryContinuationBlock
        /// </summary>
        /// <param name="de">DirectoryEntry to add</param>
        internal void AddDirectoryEntry(DirectoryEntry de)
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

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,_disk.GetNode(this,de)));
        }

        public override Task<VirtualFile> CreateFileAsync(string name, Stream dataSource)
        {
            // check if the stream is readable
            if (!dataSource.CanRead)
            {
                throw new PandaException("Stream not readable.");
            }
            return Task.Run(
                () => _createFile(name, dataSource));
        }

        private VirtualFile _createFile(string name, Stream dataSource)
        {
            // check file name
            VirtualFileSystem.CheckNodeName(name);

            // is there already a file with this name -> throw an error!
            if (Contains(name))
                throw new PathAlreadyExistsException("A file or directory with the name " + name + " already exists. Cannot create file " + _name + ".");

            // create new FileBlock
            var fb = _disk.BlockManager.AllocateFileBlock();

            // create a new DirectoryEntry and add address to FileBlock to it
            var de = new DirectoryEntry(name, fb.Offset, DirectoryEntryFlags.None);

            // create buffer with size of a data block
            var buffer = new byte[_disk.BlockManager.DataBlockSize];

            // keep track of file size
            long fileSize = 0;

            // and of how many bytes read
            int bytesRead;

            // and use a list of block offsets to data blocks
            var dataBlocks = new Queue<BlockOffset>();

            // and a current dataBlock offset

            // read entire data blocks from stream
            while ((bytesRead = dataSource.Read(buffer, 0, buffer.Length)) > 0)
            {
                // sum up file size
                fileSize += bytesRead;

                // create new data block
                var dataBlockOffset = _disk.BlockManager.AllocateDataBlock();

                // write data into data block
                _disk.BlockManager.WriteDataBlock(dataBlockOffset, buffer);

                // add address of current data block to array
                dataBlocks.Enqueue(dataBlockOffset);
            }


            // write all the data offset blocks into the files blocks

            // keep track of the current fileblock
            IFileContinuationBlock currentFileBlock = fb;

            while (dataBlocks.Count > 0)
            {
                var remainingCapacity = currentFileBlock.ListCapacity - currentFileBlock.Count;

                // Allocate the next file continuation block if necessary
                if (remainingCapacity == 0)
                {
                    var nextBlock = _disk.BlockManager.AllocateFileContinuationBlock();
                    currentFileBlock.ContinuationBlockOffset = nextBlock.Offset;
                    currentFileBlock = nextBlock;
                }

                // Take as many offsets as fit into the current block
                var draft = new List<BlockOffset>(remainingCapacity);
                while (draft.Count < remainingCapacity && dataBlocks.Count > 0)
                    draft.Add(dataBlocks.Dequeue());

                // Write offsets
                currentFileBlock.ReplaceOffsets(currentFileBlock.Append(draft).ToArray());
            }

            // add DirectoryEntry to this DirectoryBlock or a DirectoryContinuationBlock of it
            AddDirectoryEntry(de);

            // don't close the stream

            // write file size
            fb.Size = fileSize;

            // return VirtualFile
            var file = _disk.GetFile(this, de);

            _disk.BlockManager.Flush();

            // No need to notify collection changed, since AddDirectoryEntry already handled this

            return file;
        }

        public override string Name
        {
            get { return _name; }
        }

        public override long Size
        {
            get
            {
                long size = 0;

                // go trough all directoryEntries (also from ContinationBlocks) and invoke Size(), done by enumerator:
                foreach (var node in this)
                {
                    size += node.Size;
                }

                return size;
            }
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

            if (_parentDirectory.Contains(newName))
                throw new PathAlreadyExistsException("A file or directory with the name " + newName + " already exists. Cannot rename " + _name + ".");

            // search DirectoryEntry of this directory in the parent directory
            var tuple = _parentDirectory.FindDirectoryEntry(_blockOffset);

            // create new DirectoryEntry for this directory with the new name, other stuff remains unchanged
            var newDe = new DirectoryEntry(newName, tuple.Item1.BlockOffset, tuple.Item1.Flags);

            // add new DirectoryEntry
            _parentDirectory.AddDirectoryEntry(newDe);
            
            // remove old DirectoryEntry
            _parentDirectory.DeleteDirectoryEntry(tuple);

            _name = newName;

            _disk.BlockManager.Flush();

            OnPropertyChanged("Name");
            OnPropertyChanged("FullName");
        }

        /// <summary>
        /// Delete Directory: Free the DirectoryEntry in it's parent Node. Then go recursivly trough all direcotries. Invoke Delete() for each DirectoryEntry in the actual
        /// Block and all ContinuationBlocks (Deletes Files or Directories).
        /// </summary>
        public override void Delete()
        {
            // first delete the directoryEntry in the parent, so it can't be found:
            _parentDirectory.DeleteDirectoryEntry(_blockOffset);

            // go trough all directoryEntries (also from ContinationBlocks) and invoke Delete(), done by enumerator:
            foreach (var node in this)
            {
                 node.Delete();
            }

            _disk.BlockManager.Flush();

            // Notify the disk of this deletion. Necessary to keep the cache consistent
            _disk.OnDelete(this);
        }

        internal DirectoryEntry DeleteDirectoryEntry(BlockOffset offset)
        {
            return DeleteDirectoryEntry(FindDirectoryEntry(offset));
        }

        internal DirectoryEntry DeleteDirectoryEntry(Tuple<DirectoryEntry, IDirectoryContinuationBlock, IDirectoryContinuationBlock, int> tuple)
        {
            var containingBlock = tuple.Item2;
            var optPrecedingBlock = tuple.Item3;
            var entryToDelete = tuple.Item1;
            var oldEntryIndex = tuple.Item4;
            
            var oldEntry = _disk.GetNode(this, entryToDelete);
            containingBlock.DeleteEntry(entryToDelete);

            // if containing block is now is empty we link preceding block to the following Block of containing block
            // There must be a preceding block* and a continuation block after the containing block
            //      * preceding block == null means that there is no preceding block
            if (containingBlock.Count == 0 && optPrecedingBlock != null && containingBlock.ContinuationBlockOffset != null)
            {
                optPrecedingBlock.ContinuationBlockOffset = containingBlock.ContinuationBlockOffset;
                _disk.BlockManager.FreeBlock(containingBlock.Offset);
            }
            
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,oldEntry,oldEntryIndex));

            return entryToDelete;
        }

        public override void Move(VirtualDirectory destination, string newName)
        {
            if(destination == _parentDirectory)
                Rename(newName);
            else
                _move((VirtualDirectoryImpl) destination, newName);
        }

        private void _move(VirtualDirectoryImpl destination, string newName)
        {
            // check directory name
            VirtualFileSystem.CheckNodeName(newName);

            if (destination.Contains(newName))
                throw new PathAlreadyExistsException("A file or directory with the name " + newName + " already exists in " + destination.FullName + ". Cannot move " + FullName + ".");

            // search DirectoryEntry of this directory in the parent directory
            var oldDe = _parentDirectory.DeleteDirectoryEntry(_blockOffset);

            // create new DirectoryEntry for this directory with the new name, other stuff remains unchanged
            var newDe = new DirectoryEntry(newName, oldDe.BlockOffset, oldDe.Flags);

            // add new DirectoryEntry in the new destination directory
            destination.AddDirectoryEntry(newDe);

            _name = newName;
            _parentDirectory = destination;

            _disk.BlockManager.Flush();

            OnPropertyChanged("Name");
            OnPropertyChanged("FullName");
            OnPropertyChanged("ParentDirectory");
        }

        public override void Copy(VirtualDirectory destination)
        {
            _copy((VirtualDirectoryImpl) destination);
        }

        private void _copy(VirtualDirectoryImpl destination)
        {
            var newDir = destination.CreateDirectory(Name);
            foreach (var node in this)
            {
                node.Copy(newDir);
            }
        }

        public BlockOffset CacheKey
        {
            get { return _blockOffset; }
        }


    }
}
