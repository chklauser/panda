using System.IO;
using JetBrains.Annotations;

namespace Panda
{
    [PublicAPI]
    public abstract class VirtualFile : VirtualNode
    {
        public abstract Stream Open();

        public override string ToString()
        {
            return Name;
        }
    }
}
