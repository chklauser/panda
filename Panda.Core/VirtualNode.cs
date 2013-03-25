using System.IO;
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
}