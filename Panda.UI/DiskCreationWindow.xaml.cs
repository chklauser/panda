using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using Panda.UI.ViewModel;

namespace Panda.UI
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class DiskCreationWindow : Window
    {
        private readonly DiskCreationViewModel _viewModel = new DiskCreationViewModel();
        public DiskCreationWindow()
        {
            InitializeComponent();
            DataContext = _viewModel;
        }

        public DiskCreationViewModel ViewModel
        {
            get { return _viewModel; }
        }

        private void Confirm_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var capacity = ViewModel.Capacity;
            var fileName = ViewModel.FileName;
            e.CanExecute = fileName != null && !String.IsNullOrWhiteSpace(fileName) && capacity.HasValue && capacity.Value > 0;
            Trace.WriteLine("Confirm_CanExecute=" + e.CanExecute);
        }

        private void Confirm_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            DialogResult = true;
            e.Handled = true;
            Close();
        }

        private void Cancel_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Cancel_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            DialogResult = false;
            e.Handled = true;
            Close();
        }

        private void Browse_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Browse_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            var nfd = new SaveFileDialog
            {
                CheckPathExists = true,
                DefaultExt = ".panda",
                OverwritePrompt = true,
                ValidateNames = true,
                InitialDirectory = Environment.CurrentDirectory,
                AddExtension = true,
                Filter = MainWindow.FileSelectionFilter
            };
            var result = nfd.ShowDialog(this);
            if (!result.Value)
                return;

            ViewModel.FileName = nfd.FileName;
        }
    }
}
