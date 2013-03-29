using System;
using System.Runtime.InteropServices;

namespace Panda.Core.Blocks
{
    /// <summary>
    /// This is a purely semantic zero-overhead wrapper around the type we use for block offsets.
    /// We use this type to make it explicit in the type system when an API requires or provides block offsets (as opposed to ordinary integers)
    /// </summary>
    /// <remarks><para>There are explicit conversions to and from the underlying type. The conversion operators are marked 'explicit' in order
    /// to force the programmer to think twice when treating an integer as a block offset (and vice-versa)</para>
    /// <para>See `newtype` in Haskell</para></remarks>
    [StructLayout(LayoutKind.Sequential) ]
    public struct BlockOffset : IEquatable<BlockOffset>
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields",Justification = @"
This type is a low-level implementation detail. Someone who using blockOffset.Offset is already assuming that
         this member is the entire representation of the BlockOffset type (wrapper). Clients cannot and should not be shielded from
         changes in the representation of this type.
         We therefore decided not to wrap Offset in a property to make a change to the representation as breaking as possible.
        
         Performance did not play a role in this decision, as the .NET JIT compiler reliably inlines trivial 
         property accessors.
")]
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