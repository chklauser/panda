using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Panda.Core.Blocks;

namespace Panda.Core.Internal
{
    /// <summary>
    /// Root directory is special in some ways.
    /// </summary>
    class VirtualRootDirectoryImpl : VirtualDirectoryImpl
    {
        public VirtualRootDirectoryImpl(VirtualDiskImpl disk) : base(disk, disk.BlockManager.RootDirectoryBlockOffset, null, String.Empty) {}

        public override bool IsRoot
        {
            get { return true; }
        }

        public override string FullName
        {
            get { return String.Empty; }
        }

        public override void Rename(string newName)
        {
            throw new DontTouchRootException("Root directory cannot be renamed.");
        }

        public override void Move(VirtualDirectory destination, string newName)
        {
            throw new DontTouchRootException("Root directory cannot be moved.");
        }

        public override void Delete()
        {
            throw new DontTouchRootException("Root directory cannot be deleted.");
        }
    }
}
