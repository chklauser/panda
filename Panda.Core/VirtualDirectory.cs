using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Panda
{
    [PublicAPI]
    public abstract class VirtualDirectory : VirtualNode, IReadOnlyCollection<VirtualNode>, IReadOnlyDictionary<string,VirtualNode>
    {
        public abstract IEnumerator<VirtualNode> GetEnumerator();

        [PublicAPI]
        public abstract int Count { get; }

        [PublicAPI]
        public abstract bool Contains(string name);

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

        [PublicAPI]
        public abstract Task ExportAsync(string path);

        [PublicAPI]
        public virtual void Export(string path)
        {
            // Optional: provide a more efficient synchronous implementation
            ExportAsync(path).RunSynchronously();
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
        public virtual async Task<VirtualFile> CreateFileAsync([NotNull] string name, [NotNull] byte[] data,
                                                               int index = 0, int? count = null)
        {
            using (var stream = new MemoryStream(data, 0, count ?? data.Length, writable: false))
                return await CreateFileAsync(name, stream);
        }

        [PublicAPI]
        public virtual VirtualFile CreateFile([NotNull] string name, [NotNull] byte[] data, int index = 0,
                                              int? count = null)
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

        #region IEnumerable implementation

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

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

        public abstract VirtualNode this[string name] { get; }

        IEnumerable<string> IReadOnlyDictionary<string, VirtualNode>.Keys
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