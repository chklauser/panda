using System.IO;
using JetBrains.Annotations;

namespace Panda
{
    [PublicAPI]
    public abstract class VirtualNode
    {
        #region Meta information

        /// <summary>
        /// Name of current Directory/File.
        /// </summary>
        [PublicAPI]
        [NotNull]
        public abstract string Name { get; }

        /// <summary>
        /// Full path of current Directory/File.
        /// </summary>
        [PublicAPI]
        [NotNull]
        public virtual string FullName
        {
            get { return ParentDirectory.FullName + VirtualFileSystem.SeparatorChar + Name; }
        }

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

        /// <summary>
        /// Delete File/Directory (recursivly)
        /// </summary>
        [PublicAPI]
        public abstract void Delete();

        [PublicAPI]
        public abstract void Move([NotNull] VirtualDirectory destination, [NotNull] string newName);
    }
}