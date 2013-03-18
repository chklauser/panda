using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Panda
{
    [PublicAPI]
    public abstract class VirtualNode
    {
        #region Meta information

        [PublicAPI]
        [NotNull]
        public abstract string Name { get; }

        [PublicAPI]
        [NotNull]
        public abstract string FullName { get; }

        [PublicAPI]
        public abstract long Size { get; }

        [PublicAPI]
        public abstract bool IsRoot { get; }

        [PublicAPI]
        [CanBeNull]
        public abstract VirtualDirectory ParentDirectory { get; }

        #endregion

        [PublicAPI]
        public void Move([NotNull] VirtualDirectory destination)
        {
            Move(destination, Name);
        }

        [PublicAPI]
        public abstract void Rename([NotNull] string newName);

        [PublicAPI]
        public abstract void Delete();

        [PublicAPI]
        public abstract void Move([NotNull] VirtualDirectory destination, [NotNull] string newName);

    }

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
            var importTask = ImportAsync(path);
            importTask.RunSynchronously();
            return importTask.Result;
        }

        [PublicAPI]
        public abstract Task ExportAsync(string path);

        [PublicAPI]
        public virtual void Export(string path)
        {
            ExportAsync(path).RunSynchronously();
        }

        [NotNull]
        public abstract VirtualDirectory CreateDirectory([NotNull] string name);

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