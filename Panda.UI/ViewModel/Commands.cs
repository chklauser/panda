using System.Windows.Input;
using JetBrains.Annotations;

namespace Panda.UI.ViewModel
{
    // ReSharper disable InconsistentNaming
    public static class Commands
    {
        [NotNull]
        private static readonly RoutedUICommand _import = new RoutedUICommand("Import", "Import", typeof(Commands));
        public static RoutedUICommand Import
        {
            get
            {
                return _import;
            }
        }

        [NotNull]
        private static readonly RoutedUICommand _export = new RoutedUICommand("Export", "Export", typeof(Commands));
        public static RoutedUICommand Export
        {
            get
            {
                return _export;
            }
        }

        [NotNull]
        private static readonly RoutedUICommand _rename = new RoutedUICommand("Rename", "Rename", typeof(Commands));
        public static RoutedUICommand Rename
        {
            get
            {
                return _rename;
            }
        }

        [NotNull]
        private static readonly RoutedUICommand _newDirectory = new RoutedUICommand("New Directory", "NewDirectory", typeof(Commands));
        public static RoutedUICommand NewDirectory
        {
            get
            {
                return _newDirectory;
            }
        }

        [NotNull]
        private static readonly RoutedUICommand _closeDisk = new RoutedUICommand("Close Disk", "CloseDisk", typeof(Commands));

        public static RoutedUICommand CloseDisk
        {
            get { return _closeDisk; }
        }

        [NotNull] private static readonly RoutedUICommand _deleteDisk = new RoutedUICommand("Delete Disk", "DeleteDisk", typeof (Commands
            ));

        public static RoutedUICommand DeleteDisk
        {
            get { return _deleteDisk; }
        }

        public static RoutedUICommand Cancel
        {
            get { return _cancel; }
        }

        public static RoutedUICommand Browse
        {
            get { return _browse; }
        }

        [NotNull] private static readonly RoutedUICommand _cancel = new RoutedUICommand("Cancel","Cancel",typeof(DiskCreationViewModel));

        [NotNull] private static readonly RoutedUICommand _browse = new RoutedUICommand("Browse...","Browse",typeof(DiskCreationViewModel));
    }
}