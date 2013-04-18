using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Borgstrup.EditableTextBlock;
using JetBrains.Annotations;
using Microsoft.Win32;
using Panda.Core.Internal;
using Panda.UI.ViewModel;

namespace Panda.UI
{
    // ReSharper disable MemberCanBePrivate.Global
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public const string FileSelectionFilter = "Panda Virtual Disks|*.panda|All files|*.*";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _viewModel = new BrowserViewModel();
        }

        private readonly BrowserViewModel _viewModel;
        private static int _uniqueDiskNameCounter = 0;

        public BrowserViewModel ViewModel
        {
            get { return _viewModel; }
        }

        protected void ExecuteOpenDisk(object sender, ExecutedRoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
                {
                    CheckFileExists = true,
                    DefaultExt = ".panda",
                    Multiselect = true,
                    Title = "Open existing disk",
                    CheckPathExists = true,
                    InitialDirectory = Environment.CurrentDirectory,
                    Filter = FileSelectionFilter
                };
            var userClickedOk = ofd.ShowDialog(this);

            // Abort if the user wasn't in the mood to open disks after all
            if (!userClickedOk.Value) 
                return;

            // Open all the disks
            foreach (var fileName in ofd.FileNames)
                _openDisk(fileName);
        }

        private void _openDisk(string fileName)
        {
            VirtualDisk vdisk = null;
            try
            {
                vdisk = VirtualDisk.OpenExisting(fileName);
                _registerDisk(fileName, vdisk);
            }
            catch
            {
                // We weren't able to get the disk into the caring
                // hands of the view model. Close the file to prevent
                // more damage.
                if (vdisk != null)
                    vdisk.Dispose();
                throw;
            }
        }

        private void _registerDisk(string fileName, VirtualDisk vdisk)
        {
            var name = Path.GetFileNameWithoutExtension(fileName) ??
                "disk" + Interlocked.Increment(ref _uniqueDiskNameCounter);
            var diskModel = new DiskViewModel()
                {
                    Disk = vdisk,
                    Name = name
                };
            _viewModel.OpenDisks.Add(diskModel);
        }

        protected void ExecuteNewDisk(object sender, ExecutedRoutedEventArgs e)
        {
            var dialog = new DiskCreationWindow();
            var result = dialog.ShowDialog();
            if (!(result ?? false))
                return;

            var fileName = dialog.ViewModel.FileName;
            if(File.Exists(fileName))
                File.Delete(fileName);

            VirtualDisk vdisk = null;
            try
            {
                // we just use a dummy capacity here
                vdisk = VirtualDisk.CreateNew(fileName,dialog.ViewModel.Capacity ?? 10L*1024*1024);
                _registerDisk(fileName, vdisk);
            }
            catch
            {
                // We weren't able to get the disk into the caring
                // hands of the view model. Close the file to prevent
                // more damage.
                if (vdisk != null)
                    vdisk.Dispose();
                throw;
            }
        }

        protected void CanNewDisk(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        protected void ExecuteCloseDisk(object sender, ExecutedRoutedEventArgs e)
        {
            var dvm = e.Parameter as DiskViewModel;
            if (dvm != null)
            {
                ViewModel.OpenDisks.Remove(dvm);
                dvm.Disk.Dispose();
            }
        }

        protected void CanCloseDisk(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is DiskViewModel;
        }

        protected void ExecuteDeleteDisk(object sender, ExecutedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        protected void CanDeleteDisk(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        protected void CanCopy(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        protected void ExecuteCopy(object sender, ExecutedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }


        protected void ExecuteRename(object sender, ExecutedRoutedEventArgs e)
        {
            // In order to make the text block editable, we need to find the corresponding element in the tree view
            // FindVisualChild only works if the element in question is currently visible (not part of a collapsed subtree)

            var containingStackPanel = App.FindVisualChild<StackPanel>(DiskTree, e.Parameter);
            var txtBlock = LogicalTreeHelper.GetChildren(containingStackPanel).OfType<EditableTextBlock>().Single();
            txtBlock.IsInEditMode = true;
        }

        protected void CanRename(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is VirtualNode;
        }

        protected void ExecuteExport(object sender, ExecutedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        protected void CanExport(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        protected void ExecuteImport(object sender, ExecutedRoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
                {
                    CheckFileExists = true,
                    Multiselect = true,
                    Title = "Import",
                    CheckPathExists = true,
                    InitialDirectory = Environment.CurrentDirectory,
                };
            var userClickedOk = ofd.ShowDialog(this);

            // Abort if the user wasn't in the mood to import anything after all
            if (!userClickedOk.Value) 
                return;

            var dvm = e.Parameter as DiskViewModel;
            var vd = e.Parameter as VirtualDirectory;
            if (dvm != null)
            {
                // User clicked on a disk (which is wrapped in a DiskViewModel). Import all the stuff
                foreach (var fileName in ofd.FileNames)
                {
                    if ((new FileInfo(fileName)).Length < dvm.Disk.Capacity - dvm.Disk.Root.Size)
                    {
                        dvm.Disk.Root.Import("peter_new.txt");
                        ViewModel.StatusText = "Arnold was in " + dvm.Name;
                    }
                }
            }
                //ViewModel.StatusText = "Files imported in " + dvm.Name;
            else if (vd != null)
            {
                // User clicked on directory. Import all the stuff
                foreach (var fileName in ofd.FileNames)
                {
                    if ((new FileInfo(fileName)).Length < vd.getDisk().Capacity - vd.getDisk().Size)
                    {
                        vd.Import(fileName);
                    }
                }
                ViewModel.StatusText = "Files imported in " + vd.Name;
            }
        }

        protected void CanImport(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        protected void CanOpenDisk(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        protected void ExecuteCloseBrowser(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        protected void CanCloseBrowser(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            ViewModel.Dispose();
            base.OnClosed(e);
        }

        protected void ExecuteNewDirectory(object sender, ExecutedRoutedEventArgs e)
        {
            var dvm = e.Parameter as DiskViewModel;
            var vd = e.Parameter as VirtualDirectory;
            string parentName;
            if (dvm != null)
            {
                // User clicked on a disk (which is wrapped in a DiskViewModel)
                _newDirectory(dvm.Disk.Root);
                parentName = "/";
            }
            else if (vd != null)
            {
                // User clicked on directory
                _newDirectory(vd);
                parentName = vd.Name + "/";
            }
            else
            {
                return;
            }

            e.Handled = true;
            ViewModel.StatusText = "Directory created in " + parentName;
        }

        private Panda.VirtualDirectory _newDirectory(VirtualDirectory parent)
        {
            var names = new HashSet<String>(parent.ContentNames);
            var counter = 1;
            const string baseName = "new-directory";
            var name = baseName;
            while (names.Contains(name))
            {
                name = string.Format("{0}-{1}", baseName, counter);
                counter++;
            }

            var dir = parent.CreateDirectory(name);

            return dir;
        }

        protected void CanNewDirectory(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is DiskViewModel || e.Parameter is VirtualDirectory;
        }

        private void ExecuteCut(object sender, ExecutedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void CanCut(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        private void CanPaste(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = false;
        }

        private void ExecutePaste(object sender, ExecutedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void ExecuteDeleteNode(object sender, ExecutedRoutedEventArgs e)
        {
            var n = e.Parameter as VirtualNode;
            if(!(n != null && !n.IsRoot))
                return;

            VirtualDirectory parent = n.ParentDirectory;
            Debug.Assert(parent != null);
            var nextText = String.Format("Delete {0} from directory {1}.", n.Name, parent.Name);
            n.Delete();
            ViewModel.StatusText = nextText;
            var ui = e.OriginalSource as UIElement;
            if (ui != null && ui.Focusable)
                ui.Focus();
        }

        private void CanDeleteNode(object sender, CanExecuteRoutedEventArgs e)
        {
            var n = e.Parameter as VirtualNode;
            e.CanExecute = n != null && !n.IsRoot;
            e.Handled = true;
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void RenameNode_Edited(object sender, EventArgs eventArgs)
        {
            var element = sender as EditableTextBlock;
            VirtualNode node;
           if (element != null && (node = element.DataContext as VirtualNode) != null)
           {
               var newName = element.Text;
               if (node.Name == newName)
               {
                   ViewModel.StatusText = "No rename performed. Same name.";
               }
               else
               {
                   try
                   {
                       node.Rename(newName);
                   }
                   catch (Exception e)
                   {
                       ViewModel.StatusText = "Could not perform rename: " + e.Message;
                       element.Text = node.Name;
                   }
               }
           }
        }
    }
}
