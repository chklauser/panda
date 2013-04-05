using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using System;

namespace Panda
{
    [PublicAPI]
    public abstract class VirtualDirectory : VirtualNode, IReadOnlyCollection<VirtualNode>, IReadOnlyDictionary<string,VirtualNode>
    {
        /// <summary>
        /// To enable iteration over VirtualDirectory.
        /// </summary>
        /// <returns></returns>
        public abstract IEnumerator<VirtualNode> GetEnumerator();

        /// <summary>
        /// Number of nodes in VirtualDirectory.
        /// </summary>
        [PublicAPI]
        public abstract int Count { get; }

        /// <summary>
        /// Returns true if node with given name exists, else false.
        /// </summary>
        /// <param name="name">Node name</param>
        /// <returns>True if node exists in this VirtualDirectory.</returns>
        [PublicAPI]
        public abstract bool Contains(string name);

        /// <summary>
        /// Returns true if node with given name exists and writes reference into VirtualNode value, else returns false and writes null into VirtualNode value.
        /// </summary>
        /// <param name="name">Node name</param>
        /// <param name="value">VirtualNode as reference.</param>
        /// <returns></returns>
        [PublicAPI]
        public abstract bool TryGetNode(string name, out VirtualNode value);
        
        [PublicAPI]
        [NotNull]
        public abstract Task<VirtualNode> ImportAsync(string path);

        [PublicAPI]
        [NotNull]
        public virtual VirtualNode Import(string path)
        {
            // Optional: provide a more efficient synchronous implementation
            var importTask = ImportAsync(path);
            importTask.RunSynchronously();
            return importTask.Result;
        }

        #region File creation

        [NotNull]
        public abstract VirtualDirectory CreateDirectory([NotNull] string name);

        [NotNull]
        public abstract Task<VirtualFile> CreateFileAsync([NotNull] string name, [NotNull] Stream dataSource);

        [PublicAPI]
        public virtual VirtualFile CreateFile([NotNull] string name, [NotNull] Stream dataSource)
        {
            var task = CreateFileAsync(name, dataSource);
            task.RunSynchronously();
            return task.Result;
        }

        [PublicAPI]
        public async Task<VirtualFile> CreateFileAsync([NotNull] string name, [NotNull] byte[] data)
        {
            return await CreateFileAsync(name, data, 0);
        }

        [PublicAPI]
        public async Task<VirtualFile> CreateFileAsync([NotNull] string name, [NotNull] byte[] data,
                                                               int index)
        {
            return await CreateFileAsync(name, data, index, null);
        }

        [PublicAPI]
        public virtual async Task<VirtualFile> CreateFileAsync([NotNull] string name, [NotNull] byte[] data,
                                                               int index, int? count)
        {
            using (var stream = new MemoryStream(data, 0, count ?? data.Length, writable: false))
                return await CreateFileAsync(name, stream);
        }

        [PublicAPI]
        public virtual VirtualFile CreateFile([NotNull] string name, [NotNull] byte[] data)
        {
            return CreateFile(name, data, 0);
        }

        [PublicAPI]
        public virtual VirtualFile CreateFile([NotNull] string name, [NotNull] byte[] data, int index)
        {
            return CreateFile(name, data, index, null);
        }

        [PublicAPI]
        public virtual VirtualFile CreateFile([NotNull] string name, [NotNull] byte[] data, int index,
                                              int? count)
        {
            var task = CreateFileAsync(name, data, index, count);
            task.RunSynchronously();
            return task.Result;
        }

        // This method is intended for quick-and-dirty creations of small files. No asynchronous version is provided.
        // Note the absence of the PublicAPI annotation. Use this method for unit testing and small configuration files.
        public virtual VirtualFile CreateFile([NotNull] string name, string content)
        {
            return CreateFile(name, Encoding.UTF8.GetBytes(content));
        }

        #endregion

        /// <summary>
        /// Retrieve a file system node based on a relative path. Returns null if any part of the path does not exist.
        /// </summary>
        /// <param name="path">A relative path.</param>
        /// <returns>The virtual node pointed to by the path, or null if any part of the path does not exist.</returns>
        [CanBeNull]
        public abstract VirtualNode Navigate(string path);

        /// <summary>
        /// Is called by Navigate(string path) to return file system node.
        /// </summary>
        /// <param name="path">String array containing node names.</param>
        /// <returns>Virtual node pointed to by path, null if any part doesn't exist.</returns>
        [CanBeNull]
        public abstract VirtualNode Navigate(string[] path);

        /// <summary>
        /// For backwards compatibility with IEnumerable without interface.
        /// </summary>
        /// <returns></returns>
        #region IEnumerable implementation

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        /// <summary>
        /// To access nodes of directory with this["nodeName"].
        /// </summary>
        /// <returns></returns>
        #region IReadOnlyDictionary implementation

        IEnumerator<KeyValuePair<string, VirtualNode>> IEnumerable<KeyValuePair<string, VirtualNode>>.GetEnumerator()
        {
            foreach (var node in this)
                yield return new KeyValuePair<string, VirtualNode>(node.Name, node);
        }


        bool IReadOnlyDictionary<string, VirtualNode>.ContainsKey(string name)
        {
            return Contains(name);
        }

        bool IReadOnlyDictionary<string, VirtualNode>.TryGetValue(string name, out VirtualNode value)
        {
            return TryGetNode(name, out value);
        }

        // Use the same name as the interface that defines this member, even though 'key' is less appropriate here
        public VirtualNode this[string key]
        {
            get
            {
                VirtualNode node;
                if (!TryGetNode(key, out node))
                {
                    // Code analysis suggest that we use KeyNotFound exception here, and not any of our own exceptions.
                    // Given the context (this being the implementation of a dictionary member) that actually makes sense.
                    throw new KeyNotFoundException("Node not found");
                }
                return node;
            }
        }

        IEnumerable<string> IReadOnlyDictionary<string, VirtualNode>.Keys
        {
            get { return ContentNames; }
        }

        protected virtual IEnumerable<string> ContentNames
        {
            get { return this.Select<VirtualNode, string>(x => x.Name); }
        }

        IEnumerable<VirtualNode> IReadOnlyDictionary<string, VirtualNode>.Values
        {
            get { return this; }
        }

        #endregion
    }
}