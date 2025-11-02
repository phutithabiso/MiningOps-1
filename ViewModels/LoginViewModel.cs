using MiningOps.Services;
using MiningOps.Utilities;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace MiningOps.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;
        private string _username = string.Empty;
        private string _password = string.Empty;
        private bool _isLoggingIn;
        private string _errorMessage = string.Empty;

        public LoginViewModel(IAuthenticationService authService)
        {
            _authService = authService;
            LoginCommand = new RelayCommand(async () => await LoginAsync(), CanLogin);
            ClearErrorCommand = new RelayCommand(ClearError);

            Debug.WriteLine("🔐 LoginViewModel initialized");
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set => SetProperty(ref _isLoggingIn, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public ICommand LoginCommand { get; }
        public ICommand ClearErrorCommand { get; }

        public event Action? OnLoginSuccess;
        public event Action<string>? OnLoginFailed;

        private bool CanLogin()
        {
            return !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   !IsLoggingIn;
        }

        private async Task LoginAsync()
        {
            IsLoggingIn = true;
            ErrorMessage = string.Empty;

            try
            {
                Debug.WriteLine($"🔐 Attempting login for user: {Username}");

                var success = await _authService.LoginAsync(Username, Password);
                Debug.WriteLine($"🔐 Login result: {success}");

                if (success)
                {
                    Debug.WriteLine("🔐 Login successful, invoking OnLoginSuccess");
                    OnLoginSuccess?.Invoke();
                }
                else
                {
                    ErrorMessage = "Invalid username or password.";
                    Debug.WriteLine($"🔐 Login failed: {ErrorMessage}");
                    OnLoginFailed?.Invoke(ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login error: {ex.Message}";
                Debug.WriteLine($"🔐 Login exception: {ex.Message}");
                OnLoginFailed?.Invoke(ErrorMessage);
            }
            finally
            {
                IsLoggingIn = false;
                // Clear password for security
                Password = string.Empty;
                Debug.WriteLine("🔐 Login process completed");
            }
        }

        private void ClearError()
        {
            ErrorMessage = string.Empty;
        }
    }
}