using JetBrains.Annotations;

namespace Panda.UI.ViewModel
{
    public class DiskViewModel
    {
        [NotNull]
        public string Name { get; set; }

        [NotNull]
        public VirtualDisk Disk { get; set; } 
    }
}