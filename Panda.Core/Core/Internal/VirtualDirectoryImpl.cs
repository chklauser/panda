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
        private readonly VirtualDiskImpl _disk;
        private readonly BlockOffset _blockOffset;

        public VirtualDirectoryImpl(VirtualDiskImpl disk, BlockOffset blockOffset)
        {
            _disk = disk;
            _blockOffset = blockOffset;
        }

        public override VirtualNode Navigate(string path)
        {
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
                    yield return new VirtualDirectoryImpl(_disk, de.BlockOffset);
                }
                else
                {
                    yield return new VirtualFileImpl(_disk, de.BlockOffset);
                }
            }

            // do while we have continuation blocks iterate over DirectoryContinuationBlock(s) => DirectoryEntries
            while (currentDirectoryBlock.ContinuationBlock != null)
            {
                // .Value is needed because ContinuationBlock is nullable
                currentDirectoryBlock = _disk.BlockManager.GetDirectoryContinuationBlock(currentDirectoryBlock.ContinuationBlock.Value);
                foreach (DirectoryEntry de in currentDirectoryBlock)
                {
                    // DirectoryEntry tells me if file or directory
                    // return the corresponding VirtualNode
                    if (de.IsDirectory)
                    {
                        yield return new VirtualDirectoryImpl(_disk, de.BlockOffset);
                    }
                    else
                    {
                        yield return new VirtualFileImpl(_disk, de.BlockOffset);
                    }
                }
            } 
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
            throw new NotImplementedException();
        }

        public override Task<VirtualFile> CreateFileAsync(string name, System.IO.Stream dataSource)
        {
            throw new NotImplementedException();
        }

        public override VirtualNode this[string name]
        {
            get { throw new NotImplementedException(); }
        }

        public override string Name
        {
            get { throw new NotImplementedException(); }
        }

        public override string FullName
        {
            get { throw new NotImplementedException(); }
        }

        public override long Size
        {
            get { throw new NotImplementedException(); }
        }

        public override bool IsRoot
        {
            get { throw new NotImplementedException(); }
        }

        public override VirtualDirectory ParentDirectory
        {
            get { throw new NotImplementedException(); }
        }

        public override void Rename(string newName)
        {
            throw new NotImplementedException();
        }

        public override void Delete()
        {
            throw new NotImplementedException();
        }

        public override void Move(VirtualDirectory destination, string newName)
        {
            throw new NotImplementedException();
        }
    }
}
