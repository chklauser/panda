using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using JetBrains.Annotations;
using Panda.UI.ViewModel;
using ServiceStack.Service;

namespace Panda.UI
{
    /// <summary>
    /// Interaction logic for RegistrationDialog.xaml
    /// </summary>
    public partial class RegistrationDialog : Window
    {
        public RegistrationViewModel ViewModel { get; set; }

        public RegistrationDialog([NotNull] IServiceClientAsync client)
        {
            DataContext = ViewModel = new RegistrationViewModel(Dispatcher, null,client);
            InitializeComponent();
        }

        private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            var password = PasswordBox.Password;
            if (!String.IsNullOrWhiteSpace(password))
                ViewModel.UserInfo.Password = password;
            else
                Trace.WriteLine("Password was blank. Not updating model.");
        }

        private void Cancel_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !ViewModel.IsWorking;
        }

        private void Cancel_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            var rtask = ViewModel.RequestTask;
            if (rtask == null)
            {
                Trace.WriteLine("Request task is unexpectedly null.");
                return;
            }

            ViewModel.CancelRegistrationRequest();
        }

        private void Register_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (!ViewModel.IsWorking) && App.IsValid(UserNameTextBox) && App.IsValid(PasswordBox);
        }

        private async void Register_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            await ViewModel.RegisterAsync();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (ViewModel.IsWorking)
                e.Cancel = true;
            base.OnClosing(e);
        }
    }
}
