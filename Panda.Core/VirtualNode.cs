using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Panda
{
    [PublicAPI]
    public abstract class VirtualNode : INotifyPropertyChanged
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

        public abstract VirtualDisk getDisk();

        [PublicAPI]
        public abstract void Rename([NotNull] string newName);

        /// <summary>
        /// Delete File/Directory (recursivly)
        /// </summary>
        [PublicAPI]
        public abstract void Delete();

        [PublicAPI]
        public abstract void Move([NotNull] VirtualDirectory destination, [NotNull] string newName);

        [PublicAPI]
        public abstract void Copy([NotNull] VirtualDirectory destination);

        [PublicAPI]
        public abstract Task ExportAsync(string path);

        [PublicAPI]
        public virtual void Export(string path)
        {
            // Optional: provide a more efficient synchronous implementation
            ExportAsync(path).Wait();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}