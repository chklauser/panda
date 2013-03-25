using System;
using System.Runtime.InteropServices;

namespace Panda.Core.Blocks
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BlockOffset : IEquatable<BlockOffset>
    {
        public readonly uint Offset;

        public BlockOffset(uint offset) : this()
        {
            Offset = offset;
        }

        public bool Equals(BlockOffset other)
        {
            return Offset == other.Offset;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is BlockOffset && Equals((BlockOffset) obj);
        }

        public override int GetHashCode()
        {
            return (int) Offset;
        }

        public static bool operator ==(BlockOffset left, BlockOffset right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlockOffset left, BlockOffset right)
        {
            return !left.Equals(right);
        }

        public static explicit operator uint(BlockOffset offset)
        {
            return offset.Offset;
        }

        public static explicit operator BlockOffset(uint offset)
        {
            return new BlockOffset(offset);
        }

        public override string ToString()
        {
            return String.Format("B+{0}", Offset);
        }
    }
}