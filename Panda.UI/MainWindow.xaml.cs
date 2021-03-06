﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Borgstrup;
using JetBrains.Annotations;
using Microsoft.WindowsAPICodePack.Dialogs;
using Panda.Core.Internal;
using Panda.ServiceModel;
using Panda.UI.ViewModel;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Panda.UI
{
    // ReSharper disable MemberCanBePrivate.Global
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public const string FileSelectionFilter = "Panda Virtual Disks|*.panda|All files|*.*";

        private Collection<VirtualNode> _pasteBufferNodes = new Collection<VirtualNode>();
        private VirtualDisk _pasteBufferDisk;
        private Boolean _isCut;

        public MainWindow()
        {
            DataContext = _viewModel = new BrowserViewModel();
            InitializeComponent();
        }

        private readonly BrowserViewModel _viewModel;

        public BrowserViewModel ViewModel
        {
            get { return _viewModel; }
        }

        public Collection<VirtualNode> PasteBufferNodes
        {
            get { return _pasteBufferNodes; }
        }

        public VirtualDisk PasteBufferDisk
        {
            get { return _pasteBufferDisk; }
            set { _pasteBufferDisk = value; }
        }

        public bool IsCut
        {
            get { return _isCut; }
            set { _isCut = value; }
        }

        protected void ExecuteOpenDisk(object sender, ExecutedRoutedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            
            var record = e.Parameter as DiskRecord;
            if (record != null)
            {
                ViewModel.OpenDisk(record.Name + ".panda",Dispatcher);
            }
            else
            {
                // Open an unassociated disk
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
                    ViewModel.OpenDisk(fileName, Dispatcher);
            }
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
                ViewModel.RegisterDisk(fileName, vdisk,Dispatcher);
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
            if (e == null)
                throw new ArgumentNullException("e");
            
            e.CanExecute = true;
        }

        protected void ExecuteCloseDisk(object sender, ExecutedRoutedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            
            var dvm = e.Parameter as DiskViewModel;
            if (dvm != null)
            {
                ViewModel.OpenDisks.Remove(dvm);
                dvm.Disk.Dispose();
            }
        }

        protected void CanCloseDisk(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            
            e.CanExecute = e.Parameter is DiskViewModel;
        }

        protected void ExecuteDeleteDisk(object sender, ExecutedRoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        protected void CanDeleteDisk(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            
            e.CanExecute = false;
        }

        protected void CanCopy(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            
            var vd = e.Parameter as VirtualNode;
            if (vd != null)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        protected void ExecuteCopy(object sender, ExecutedRoutedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            
            // copy can only happen on a virtualnode
            var vn = e.Parameter as VirtualNode;
            if (vn != null)
            {
                // buffer from which disk the node is
                _pasteBufferDisk = vn.Disk;
                // empty _pasteBufferNodes   
                _pasteBufferNodes.Clear();
                // add selected node to _pasteBufferNodes
                _pasteBufferNodes.Add(vn);
                _isCut = false;
            }
            else
            {
                throw new PandaException("No virtualnode clicked.");
            }
        }

        private void CanCut(object sender, CanExecuteRoutedEventArgs e)
        {
            var vd = e.Parameter as VirtualNode;
            if (vd != null)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void ExecuteCut(object sender, ExecutedRoutedEventArgs e)
        {
            // copy can only happen on a virtualnode
            var vn = e.Parameter as VirtualNode;
            if (vn != null)
            {
                // buffer from which disk the node is
                _pasteBufferDisk = vn.Disk;
                // empty _pasteBufferNodes   
                _pasteBufferNodes.Clear();
                // add selected node to _pasteBufferNodes
                _pasteBufferNodes.Add(vn);
                _isCut = true;
            }
            else
            {
                throw new PandaException("No virtualnode clicked.");
            }
        }

        private void CanPaste(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_pasteBufferNodes.Count > 0)
            {
                var targetDirectory = e.Parameter as VirtualDirectory;
                var targetDisk = e.Parameter as DiskViewModel;
                if (targetDirectory != null)
                {
                    e.CanExecute = true;
                }
                else if (targetDisk != null)
                {
                    e.CanExecute = true;
                }
                else
                {
                    e.CanExecute = false;
                }
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void ExecutePaste(object sender, ExecutedRoutedEventArgs e)
        {
            // if user clicked on a disk, the directory is the root directory
            var dvm = e.Parameter as DiskViewModel;
            var targetDirectory = e.Parameter as VirtualDirectory;
            if (dvm != null)
            {
                targetDirectory = dvm.Disk.Root;
            }

            // paste can only happen on a virtualdirectory
            
            if (targetDirectory != null)
            {
                if (targetDirectory.Disk == _pasteBufferDisk)
                {
                    // _pasteBufferNodes can also be a collection of VirtualDirectories
                    foreach (VirtualNode buffernode in _pasteBufferNodes)
                    {
                        try
                        {
                            if (_isCut)
                            {
                                buffernode.Move(targetDirectory);
                            }
                            else
                            {
                                buffernode.Copy(targetDirectory);
                            }
                        }
                        catch (Panda.Core.PathAlreadyExistsException)
                        {
                            ViewModel.StatusText = "Paste failed because a node with the same name already exists.";
                            return;
                        }
                    }
                }
                else
                {
                    try
                    {
                        // paste is on different disk
                        // _pasteBufferNodes can also be a collection of VirtualDirectories
                        foreach (VirtualNode buffernode in _pasteBufferNodes)
                        {
                            _doPasteToDifferentDisk(buffernode, targetDirectory);
                            // if the values were cutted, not copied, we have to delete them too
                            if (_isCut)
                            {
                                buffernode.Delete();
                            }
                        }
                    }
                    catch (Panda.Core.PathAlreadyExistsException)
                    {
                        ViewModel.StatusText = "Paste failed because a node with the same name already exists.";
                        return;
                    }
                }
            }
            else
            {
                throw new PandaException("Paste on no virtualdirectory");
            }
            ViewModel.StatusText = "Paste successful.";
        }

        private void _doPasteToDifferentDisk(VirtualNode source, VirtualDirectory target)
        {
            // check if source is a directory
            var vd = source as VirtualDirectory;
            if (vd != null)
            {
                // yes => create directory & recursively call this function for all child nodes
                var newVd = ((VirtualDirectory) target).CreateDirectory(source.Name);
                foreach (VirtualNode child in vd)
                {
                    _doPasteToDifferentDisk(child, newVd);
                }
            }
            else
            {
                // no => copy file
                target.CreateFile(source.Name, ((VirtualFile) source).Open());
            }
        }

        protected void ExecuteRename(object sender, ExecutedRoutedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            
            // In order to make the text block editable, we need to find the corresponding element in the tree view
            // FindVisualChild only works if the element in question is currently visible (not part of a collapsed subtree)

            var containingStackPanel = App.FindVisualChild<StackPanel>(DiskTree, e.Parameter);
            var txtBlock = LogicalTreeHelper.GetChildren(containingStackPanel).OfType<EditableTextBlock>().Single();
            txtBlock.IsInEditMode = true;
        }

        protected void CanRename(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            
            e.CanExecute = e.Parameter is VirtualNode;
        }

        protected void ExecuteExport(object sender, ExecutedRoutedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            
            var fbd = new FolderBrowserDialog
            {
                SelectedPath = Environment.CurrentDirectory,
            };

            var dialogResult = fbd.ShowDialog();
            if (dialogResult != System.Windows.Forms.DialogResult.OK 
                && dialogResult != System.Windows.Forms.DialogResult.Yes)
                return;

            var dvm = e.Parameter as DiskViewModel;
            var vd = e.Parameter as VirtualDirectory;
            var vf = e.Parameter as VirtualFile;
            string name;
            if (dvm != null)
            {
                // User clicked on a disk (which is wrapped in a DiskViewModel). Export all the stuff
                dvm.Disk.Root.Export(fbd.SelectedPath);
                name = dvm.Name;
            }
            else if (vd != null)
            {
                // User clicked on directory. Export all the stuff
                vd.Export(fbd.SelectedPath);
                name = vd.Name;
            }
            else if (vf != null)
            {
                // User clicked on directory. Export all the stuff
                vf.Export(fbd.SelectedPath);
                name = vf.Name;
            }
            else
            {
                return;
            }

            ViewModel.StatusText = "Files exported from " + name;
        }

        protected void CanExport(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            
            e.CanExecute = true;
        }

        protected async void ExecuteImport(object sender, ExecutedRoutedEventArgs e)
        {
            const string folderSelectionSentinel = "Folder Selection.";
            var ofd = new CommonOpenFileDialog("Import file or folder."){AddToMostRecentlyUsedList = false,AllowNonFileSystemItems = false,RestoreDirectory = true,DefaultFileName = folderSelectionSentinel};
            
            var dialogResult = ofd.ShowDialog(this);

            // Abort if the user wasn't in the mood to import anything after all
            if (dialogResult != CommonFileDialogResult.Ok) 
                return;

            var dvm = e.Parameter as DiskViewModel;
            var vd = e.Parameter as VirtualDirectory;
            VirtualDirectory targetDirectory;
            if (dvm != null)
            {
                // User clicked on a disk (which is wrapped in a DiskViewModel).
                targetDirectory = dvm.Disk.Root;
            }
            else if (vd != null)
            {
                targetDirectory = vd;
            }
            else
            {
                ViewModel.StatusText = "No disk or directory selected. Unable to import.";
                return;
            }

            // Perform actual import
            try
            {
                foreach (var fileName in ofd.FileNames)
                {
                    var actualPath = fileName;
                    if (actualPath.EndsWith(folderSelectionSentinel))
                        actualPath = Path.GetDirectoryName(actualPath);
                    await targetDirectory.ImportAsync(actualPath);
                }
                ViewModel.StatusText = "Files imported into " + _getDiskName(targetDirectory) + ":" + targetDirectory.FullName;
            }
            catch (Exception ex)
            {
                ViewModel.StatusText = "Import failed: " + _getHumanExceptionMessage(ex);
                Trace.WriteLine("Exception during import: " + ex);
            }
        }

        private string _getHumanExceptionMessage(Exception ex)
        {
            var aggregateException = ex as AggregateException;
            var targetInvocationException = ex as TargetInvocationException;

            if (aggregateException != null)
            {
                aggregateException = aggregateException.Flatten();
                return _getHumanExceptionMessage(aggregateException.InnerException);
            }
            else if (targetInvocationException != null)
            {
                return _getHumanExceptionMessage(targetInvocationException.InnerException);
            }
            else
            {
                return ex.Message;
            }
        }

        private object _getDiskName(VirtualNode virtualNode)
        {
            return ViewModel.OpenDisks.First(dvm => dvm.Disk == virtualNode.Disk).Name;
        }

        protected void CanImport(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            
            e.CanExecute = true;
        }

        protected void CanOpenDisk(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            
            var record = e.Parameter as DiskRecord;
            e.CanExecute = record == null || File.Exists(record.Name + ".panda");
        }

        protected void ExecuteCloseBrowser(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        protected void CanCloseBrowser(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            
            e.CanExecute = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            ViewModel.Dispose();
            base.OnClosed(e);
        }

        protected void ExecuteNewDirectory(object sender, ExecutedRoutedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            
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

        private static void _newDirectory(VirtualDirectory parent)
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

            parent.CreateDirectory(name);
        }

        protected void CanNewDirectory(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e == null)
                throw new ArgumentNullException("e");
            
            e.CanExecute = e.Parameter is DiskViewModel || e.Parameter is VirtualDirectory;
        }

        private void ExecuteDeleteNode(object sender, ExecutedRoutedEventArgs e)
        {
            var n = e.Parameter as VirtualNode;
            if(!(n != null && !n.IsRoot))
                return;

            var parent = n.ParentDirectory;
            Debug.Assert(parent != null);
            n.Delete();
            ViewModel.StatusText = String.Format("Delete {0} from directory {1}.", n.Name, parent.Name);
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode",Justification = "False positive. The method is bound to the Edited event of the EditableTextBlock custom control.")]
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

        private void CanSearch(object sender, CanExecuteRoutedEventArgs e)
        {
            var vdir = e.Parameter as VirtualDirectory;
            var vdisk = e.Parameter as DiskViewModel;
            if (vdir != null)
            {
                e.CanExecute = true;
            }
            else if (vdisk != null)
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void ExecuteSearch(object sender, ExecutedRoutedEventArgs e)
        {
            SearchWindow dialog;
            var vdir = e.Parameter as VirtualDirectory;
            var vdisk = e.Parameter as DiskViewModel;
            if (vdir != null)
            {
                dialog = new SearchWindow(vdir);
            }
            else if (vdisk != null)
            {
                dialog = new SearchWindow(vdisk.Disk.Root);
            }
            else
            {
                throw new PandaException("Invalid parameter to search command. Expecting disk or directory.");
            }

            dialog.ShowDialog();

            // ideally we would like to display the search result in the main browser
            // but the TreeView does not make it easy to select a particular node.
        }

        private async void ExecuteConnect(object sender, ExecutedRoutedEventArgs e)
        {
            await ViewModel.ConnectAsync();
        }

        private void CanConnect(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ViewModel.CanConnect
                && App.IsValid(ServerUrlTextBox);
        }

        private async void ExecuteRefresh(object sender, ExecutedRoutedEventArgs e)
        {
            await ViewModel.RefreshServerDisksAsync();
        }

        private void CanRefresh(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ViewModel.IsConnected;
        }

        private async void ExecuteAssociate(object sender, ExecutedRoutedEventArgs e)
        {
            var diskView = e.Parameter as DiskViewModel;

            if (diskView == null)
            {
                Trace.TraceError("Disconnect command parameter is not DiskViewModel.");
                return;
            }

            await ViewModel.AssociateDiskAsync(diskView);
        }

        private void CanAssociate(object sender, CanExecuteRoutedEventArgs e)
        {
            var diskView = e.Parameter as DiskViewModel;

            e.CanExecute = diskView != null && ViewModel.CanAssociateDisk(diskView);
        }

        private void ExecuteDisconnectDisk(object sender, ExecutedRoutedEventArgs e)
        {
            var diskView = e.Parameter as DiskViewModel;

            if (diskView == null)
            {
                Trace.TraceError("Disconnect command parameter is not DiskViewModel.");
                return;
            }

            diskView.Disconnect();
        }

        private void CanDisconnectDisk(object sender, CanExecuteRoutedEventArgs e)
        {
            var diskView = e.Parameter as DiskViewModel;

            e.CanExecute = diskView != null && diskView.CanDisconnect;
        }

        private async void ExecuteDownloadDisk(object sender, ExecutedRoutedEventArgs e)
        {
            await ViewModel.DownloadDisk((DiskRecord) e.Parameter,Dispatcher);
        }

        private void CanDownloadDisk(object sender, CanExecuteRoutedEventArgs e)
        {
            var diskRecord = e.Parameter as DiskRecord;
            e.CanExecute = diskRecord != null && ViewModel.CanDownloadDiks(diskRecord);
        }

        private void ExecuteDisconnectServer(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.Disconnect();
        }

        private void CanDisconnectServer(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ViewModel.IsConnected;
        }

        private void ExecuteSynchronize(object sender, ExecutedRoutedEventArgs e)
        {
            ViewModel.Synchronize((DiskViewModel) e.Parameter);
        }

        private void CanSynchronize(object sender, CanExecuteRoutedEventArgs e)
        {
            var diskRecord = e.Parameter as DiskViewModel;
            e.CanExecute = diskRecord != null && ViewModel.CanSynchronize(diskRecord);
        }
    }
}
