using System;
using JetBrains.Annotations;

namespace Panda.Core.Blocks
{
    /// <summary>
    /// A representation of an individual entry in a directory.
    /// </summary>
    /// <remarks>This class exhibits immutable value semantics. It cannot be used to update directory entries in a directory block, as blocks are changed as a whole.</remarks>
    public sealed class DirectoryEntry : IEquatable<DirectoryEntry>
    {
        [NotNull]
        private readonly string _name;

        private readonly BlockOffset _blockOffset;

        private readonly DirectoryEntryFlags _flags;

        public DirectoryEntry([NotNull] string name, BlockOffset blockOffset, DirectoryEntryFlags flags)
        {
            if (name == null) throw new ArgumentNullException("name");
            _name = name;
            _blockOffset = blockOffset;
            _flags = flags;
        }

        [NotNull]
        public string Name
        {
            get { return _name; }
        }

        public BlockOffset BlockOffset
        {
            get { return _blockOffset; }
        }

        public DirectoryEntryFlags Flags
        {
            get
            {
                return _flags;
            }
        }

        public bool IsDirectory
        {
            get { return (Flags & DirectoryEntryFlags.Directory) == DirectoryEntryFlags.Directory; }
        }

        public bool Equals(DirectoryEntry other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(_name, other._name) && _blockOffset == other._blockOffset;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is DirectoryEntry && Equals((DirectoryEntry) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_name.GetHashCode()*397) ^ _blockOffset.GetHashCode();
            }
        }
    }

    [Flags]
    public enum DirectoryEntryFlags : byte
    {
        Directory = 1
    }
}