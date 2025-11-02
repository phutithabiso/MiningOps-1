using MiningOps.Models.Entities;
using MiningOps.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace MiningOps.ViewModels
{
    public class UserProfileViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;
        private readonly UserService _userService;
        private RegisterMining _user;
        private string _currentPassword = string.Empty;
        private string _newPassword = string.Empty;
        private string _confirmPassword = string.Empty;
        private bool _passwordsMatch;
        private bool _showPasswordMatch;
        private string _passwordMatchText = string.Empty;
        private Color _passwordMatchColor = Colors.Gray;
        private ObservableCollection<string> _validationErrors = new ObservableCollection<string>();

        public UserProfileViewModel(IAuthenticationService authService, UserService userService)
        {
            _authService = authService;
            _userService = userService;
            _user = authService.CurrentUser;

            // Create a copy of the user for editing
            User = new RegisterMining
            {
                AccId = _user.AccId,
                Username = _user.Username,
                FullName = _user.FullName,
                Email = _user.Email,
                PhoneNumber = _user.PhoneNumber,
                Role = _user.Role
            };
        }

        public RegisterMining User
        {
            get => _user;
            set => SetProperty(ref _user, value);
        }

        public string CurrentPassword
        {
            get => _currentPassword;
            set => SetProperty(ref _currentPassword, value);
        }

        public string NewPassword
        {
            get => _newPassword;
            set
            {
                SetProperty(ref _newPassword, value);
                ValidatePasswordMatch();
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                SetProperty(ref _confirmPassword, value);
                ValidatePasswordMatch();
            }
        }

        public bool PasswordsMatch
        {
            get => _passwordsMatch;
            set => SetProperty(ref _passwordsMatch, value);
        }

        public bool ShowPasswordMatch
        {
            get => _showPasswordMatch;
            set => SetProperty(ref _showPasswordMatch, value);
        }

        public string PasswordMatchText
        {
            get => _passwordMatchText;
            set => SetProperty(ref _passwordMatchText, value);
        }

        public Color PasswordMatchColor
        {
            get => _passwordMatchColor;
            set => SetProperty(ref _passwordMatchColor, value);
        }

        public ObservableCollection<string> ValidationErrors
        {
            get => _validationErrors;
            set => SetProperty(ref _validationErrors, value);
        }

        public bool HasValidationErrors => ValidationErrors.Any();
        public bool CanSave => !HasValidationErrors && (string.IsNullOrEmpty(NewPassword) || PasswordsMatch);

        public void ValidatePasswordMatch()
        {
            if (string.IsNullOrEmpty(NewPassword) && string.IsNullOrEmpty(ConfirmPassword))
            {
                ShowPasswordMatch = false;
                return;
            }

            ShowPasswordMatch = true;
            PasswordsMatch = NewPassword == ConfirmPassword;

            if (PasswordsMatch)
            {
                PasswordMatchText = "Passwords match";
                PasswordMatchColor = Colors.Green;
            }
            else
            {
                PasswordMatchText = "Passwords do not match";
                PasswordMatchColor = Colors.Red;
            }

            OnPropertyChanged(nameof(CanSave));
        }

        public bool ValidateAndSave()
        {
            ValidationErrors.Clear();

            // Basic field validation
            if (string.IsNullOrWhiteSpace(User.FullName))
                ValidationErrors.Add("Full Name is required");

            if (string.IsNullOrWhiteSpace(User.Username))
                ValidationErrors.Add("Username is required");

            if (string.IsNullOrWhiteSpace(User.Email))
                ValidationErrors.Add("Email is required");

            if (string.IsNullOrWhiteSpace(User.PhoneNumber))
                ValidationErrors.Add("Phone Number is required");

            // Password validation
            if (!string.IsNullOrEmpty(NewPassword))
            {
                if (NewPassword.Length < 6)
                {
                    ValidationErrors.Add("New password must be at least 6 characters long");
                }

                if (!PasswordsMatch)
                {
                    ValidationErrors.Add("New password and confirmation do not match");
                }
            }

            // Email format validation
            if (!string.IsNullOrEmpty(User.Email) && !System.Text.RegularExpressions.Regex.IsMatch(User.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ValidationErrors.Add("Please enter a valid email address");
            }

            OnPropertyChanged(nameof(HasValidationErrors));
            OnPropertyChanged(nameof(CanSave));

            return !HasValidationErrors;
        }
    }
}