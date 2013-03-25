using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Panda.Core.Internal
{
    class VirtualFileImpl : VirtualFile
    {
        public override System.IO.Stream Open()
        {
            throw new NotImplementedException();
        }

        public override string Name
        {
            get { throw new NotImplementedException(); }
        }

        public override string FullName
        {
            get { throw new NotImplementedException(); }
        }

        public override long Size
        {
            get { throw new NotImplementedException(); }
        }

        public override bool IsRoot
        {
            get { throw new NotImplementedException(); }
        }

        public override VirtualDirectory ParentDirectory
        {
            get { throw new NotImplementedException(); }
        }

        public override void Rename(string newName)
        {
            throw new NotImplementedException();
        }

        public override void Delete()
        {
            throw new NotImplementedException();
        }

        public override void Move(VirtualDirectory destination, string newName)
        {
            throw new NotImplementedException();
        }
    }
}
