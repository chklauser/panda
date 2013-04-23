using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using JetBrains.Annotations;
using ServiceStack.Service;
using ServiceStack.ServiceInterface.Auth;

namespace Panda.UI.ViewModel
{
    public class RegistrationViewModel : INotifyPropertyChanged
    {
        [NotNull]
        private readonly Dispatcher _dispatcher;
        private Registration _userInfo;
        private RegistrationResponse _registrationResponse;
        [NotNull]
        private readonly IServiceClientAsync _client;
        private TaskCompletionSource<RegistrationResponse> _requestTaskSource;

        public RegistrationViewModel([NotNull] Dispatcher dispatcher, Registration userInfo, [NotNull] IServiceClientAsync client)
        {
            if (dispatcher == null)
                throw new ArgumentNullException("dispatcher");
            
            _dispatcher = dispatcher;
            _client = client;
            _userInfo = userInfo ?? new Registration();
        }

        public Registration UserInfo
        {
            get { return _userInfo; }
            set
            {
                if (Equals(value, _userInfo)) return;
                _userInfo = value;
                OnPropertyChanged();
            }
        }

        public bool IsEnabled { get { return RequestTask == null; } }
        public bool IsWorking { get { return RequestTask != null; } }

        [CanBeNull]
        public Task<RegistrationResponse> RequestTask
        {
            get { return _requestTaskSource == null ? null : _requestTaskSource.Task; }
        }

        public async Task<RegistrationResponse> RegisterAsync()
        {
            var resp = RegistrationResponse = await _sendRegistrationRequestAsync();
            RequestTaskSource = null;
            return resp;
        }

        public void CancelRegistrationRequest()
        {
            if(RequestTaskSource == null)
                return;
            
            _client.CancelAsync();
            RequestTaskSource.TrySetCanceled(); // may fail if service stack already raised a cancellation exception
            RequestTaskSource = null;
        }

        private Task<RegistrationResponse> _sendRegistrationRequestAsync()
        {
            if (RequestTaskSource != null)
                throw new InvalidOperationException("Can only have one pending registration request at a time.");
            RegistrationResponse = null;
            RequestTaskSource = new TaskCompletionSource<RegistrationResponse>();
            _client.SendAsync<RegistrationResponse>(UserInfo, RequestTaskSource.SetResult,
                _onAsyncRegistrationError);
            return RequestTaskSource.Task;
        }

        private void _onAsyncRegistrationError(RegistrationResponse resp, Exception ex)
        {
            // May fail if the task has been cancelled before.
            RequestTaskSource.TrySetException(ex);
        }

        public RegistrationResponse RegistrationResponse
        {
            get { return _registrationResponse; }
            set
            {
                if (Equals(value, _registrationResponse)) return;
                _registrationResponse = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #region View-specific commands

        [NotNull] private static readonly RoutedUICommand _register =
            new RoutedUICommand("Register", "Register", typeof (RegistrationViewModel
                ));

        public static RoutedUICommand Register
        {
            get { return _register; }
        }

        #endregion
        
        protected TaskCompletionSource<RegistrationResponse> RequestTaskSource
        {
            get { return _requestTaskSource; }
            set
            {
                if (Equals(value, _requestTaskSource)) return;
                _requestTaskSource = value;
                OnPropertyChanged();
                OnPropertyChanged("RequestTask");
                OnPropertyChanged("IsWorking");
                OnPropertyChanged("IsEnabled");
            }
        }
    }
}