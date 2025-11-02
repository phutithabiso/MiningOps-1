using MiningOps.Models.Entities;
using MiningOps.Services;
using MiningOps.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace MiningOps.ViewModels
{
    public class UserDialogViewModel : BaseViewModel
    {
        private readonly UserService _userService;
        private RegisterMining _user;
        private string _currentPassword = string.Empty;
        private string _newPassword = string.Empty;
        private string _confirmPassword = string.Empty;
        private bool _isNewUser;
        private bool _passwordsMatch;
        private bool _showPasswordStrength;
        private bool _showPasswordMatch;
        private string _passwordStrengthText = string.Empty;
        private string _passwordMatchText = string.Empty;
        private Color _passwordStrengthColor = Colors.Gray;
        private Color _passwordMatchColor = Colors.Gray;
        private ObservableCollection<string> _validationErrors = new ObservableCollection<string>();
        private bool _isPasswordChangeRequested = false;
        public bool ShouldShowPasswordFields => IsNewUser || IsPasswordChangeRequested;
        public UserDialogViewModel(UserService userService, RegisterMining user = null)
        {
            _userService = userService;
            _user = user ?? new RegisterMining();
            _isNewUser = user == null;

            // Set default values for new users
            if (_isNewUser)
            {
                _user.Role = UserRole.Supervisor; // Default role
                _isPasswordChangeRequested = true; // Password required for new users
            }

            // Initialize commands
            ValidatePasswordCommand = new RelayCommand(ValidatePasswordStrength);
            ValidateMatchCommand = new RelayCommand(ValidatePasswordMatch);
            SaveCommand = new RelayCommand(SaveUser, () => CanSave);
            CancelCommand = new RelayCommand(Cancel);

            // Set up property change tracking for validation
            PropertyChanged += OnPropertyChanged;
        }
        private void RefreshCommands()
        {
            (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        public RegisterMining User
        {
            get => _user;
            set
            {
                if (SetProperty(ref _user, value))
                {
                    ValidateForm();
                }
            }
        }

        public string DialogTitle => IsNewUser ? "Create New User" : "Edit User";

        public bool IsNewUser
        {
            get => _isNewUser;
            set => SetProperty(ref _isNewUser, value);
        }

        public bool IsPasswordChangeRequested
        {
            get => _isPasswordChangeRequested;
            set
            {
                if (SetProperty(ref _isPasswordChangeRequested, value))
                {
                    ValidateForm();
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        public string CurrentPassword
        {
            get => _currentPassword;
            set
            {
                SetProperty(ref _currentPassword, value);
                ValidateForm();
            }
        }

        public string NewPassword
        {
            get => _newPassword;
            set
            {
                SetProperty(ref _newPassword, value);
                ValidatePasswordStrength();
                ValidatePasswordMatch();
                ValidateForm();
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                SetProperty(ref _confirmPassword, value);
                ValidatePasswordMatch();
                ValidateForm();
            }
        }

        public bool PasswordsMatch
        {
            get => _passwordsMatch;
            set => SetProperty(ref _passwordsMatch, value);
        }

        public bool ShowPasswordStrength
        {
            get => _showPasswordStrength;
            set => SetProperty(ref _showPasswordStrength, value);
        }

        public bool ShowPasswordMatch
        {
            get => _showPasswordMatch;
            set => SetProperty(ref _showPasswordMatch, value);
        }

        public string PasswordStrengthText
        {
            get => _passwordStrengthText;
            set => SetProperty(ref _passwordStrengthText, value);
        }

        public string PasswordMatchText
        {
            get => _passwordMatchText;
            set => SetProperty(ref _passwordMatchText, value);
        }

        public Color PasswordStrengthColor
        {
            get => _passwordStrengthColor;
            set => SetProperty(ref _passwordStrengthColor, value);
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

        // FIXED: Proper CanSave logic that updates dynamically
        public bool CanSave => !HasValidationErrors &&
                              (!string.IsNullOrWhiteSpace(User.FullName)) &&
                              (!string.IsNullOrWhiteSpace(User.Username)) &&
                              (!string.IsNullOrWhiteSpace(User.Email)) &&
                              (!string.IsNullOrWhiteSpace(User.PhoneNumber)) &&
                              (IsNewUser ? (!string.IsNullOrEmpty(NewPassword) && PasswordsMatch) :
                                (!IsPasswordChangeRequested || (!string.IsNullOrEmpty(NewPassword) && PasswordsMatch)));

        public ICommand ValidatePasswordCommand { get; }
        public ICommand ValidateMatchCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event System.Action<bool> RequestClose;

        // FIXED: Track user property changes for validation
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(User) ||
                e.PropertyName == nameof(User.FullName) ||
                e.PropertyName == nameof(User.Username) ||
                e.PropertyName == nameof(User.Email) ||
                e.PropertyName == nameof(User.PhoneNumber) ||
                e.PropertyName == nameof(User.Role))
            {
                ValidateForm();
            }
        }

        // FIXED: Comprehensive form validation
        private void ValidateForm()
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

            // Email format validation
            if (!string.IsNullOrEmpty(User.Email) && !System.Text.RegularExpressions.Regex.IsMatch(User.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ValidationErrors.Add("Please enter a valid email address");
            }

            // Phone number basic validation (at least 10 digits)
            if (!string.IsNullOrEmpty(User.PhoneNumber))
            {
                var digitsOnly = new string(User.PhoneNumber.Where(char.IsDigit).ToArray());
                if (digitsOnly.Length < 10)
                {
                    ValidationErrors.Add("Phone number must contain at least 10 digits");
                }
            }

            // FIXED: Password validation for new users
            if (IsNewUser)
            {
                if (string.IsNullOrEmpty(NewPassword))
                {
                    ValidationErrors.Add("Password is required for new users");
                }
                else if (NewPassword.Length < 6)
                {
                    ValidationErrors.Add("Password must be at least 6 characters long");
                }
                else if (!PasswordsMatch)
                {
                    ValidationErrors.Add("Password and confirmation do not match");
                }
            }

            // FIXED: Password validation for existing users changing password
            if (!IsNewUser && IsPasswordChangeRequested)
            {
                // Only validate if user actually checked "Change Password"
                if (IsPasswordChangeRequested)
                {
                    if (string.IsNullOrEmpty(NewPassword))
                    {
                        ValidationErrors.Add("New password is required when changing password");
                    }
                    else if (NewPassword.Length < 6)
                    {
                        ValidationErrors.Add("New password must be at least 6 characters long");
                    }
                    else if (!PasswordsMatch)
                    {
                        ValidationErrors.Add("New password and confirmation do not match");
                    }
                }
                // If not changing password, no password validation needed
            }

            OnPropertyChanged(nameof(HasValidationErrors));
            OnPropertyChanged(nameof(CanSave));
            RefreshCommands();
        }

       

        public void ValidatePasswordStrength()
        {
            if (string.IsNullOrEmpty(NewPassword))
            {
                ShowPasswordStrength = false;
                return;
            }

            ShowPasswordStrength = true;

            // Simple password strength validation
            if (NewPassword.Length < 6)
            {
                PasswordStrengthText = "Weak - Too short";
                PasswordStrengthColor = Colors.Red;
            }
            else if (NewPassword.Length < 8)
            {
                PasswordStrengthText = "Fair - Could be stronger";
                PasswordStrengthColor = Colors.Orange;
            }
            else if (System.Text.RegularExpressions.Regex.IsMatch(NewPassword, @"^(?=.*[a-zA-Z])(?=.*\d).{8,}$"))
            {
                PasswordStrengthText = "Strong - Good password";
                PasswordStrengthColor = Colors.Green;
            }
            else
            {
                PasswordStrengthText = "Medium - Add numbers and letters";
                PasswordStrengthColor = Colors.Orange;
            }

            OnPropertyChanged(nameof(CanSave));
        }

        public void ValidatePasswordMatch()
        {
            if (string.IsNullOrEmpty(NewPassword) && string.IsNullOrEmpty(ConfirmPassword))
            {
                ShowPasswordMatch = false;
                return;
            }

            ShowPasswordMatch = true;
            PasswordsMatch = NewPassword == ConfirmPassword;

            if (PasswordsMatch && !string.IsNullOrEmpty(NewPassword))
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

        private void SaveUser()
        {
            // Final validation before saving
            ValidateForm();

            if (!CanSave)
            {
                MessageBox.Show("Please fix all validation errors before saving.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidateAndSave())
            {
                return;
            }

            RequestClose?.Invoke(true);
        }

        private void Cancel()
        {
            RequestClose?.Invoke(false);
        }

        public bool ValidateAndSave()
        {
            ValidateForm();

            if (HasValidationErrors)
            {
                var errorMessage = "Please fix the following errors:\n• " + string.Join("\n• ", ValidationErrors);
                MessageBox.Show(errorMessage, "Validation Errors",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            try
            {
                Debug.WriteLine($"✅ User validated: {User.Username}, Role: {User.Role}, Password Changed: {(!IsNewUser && IsPasswordChangeRequested)}");
                return true;
            }
            catch (System.Exception ex)
            {
                ValidationErrors.Add($"Save failed: {ex.Message}");
                OnPropertyChanged(nameof(HasValidationErrors));

                MessageBox.Show($"Error saving user: {ex.Message}", "Save Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public Dictionary<UserRole, string> UserRoles => new Dictionary<UserRole, string>
        {
            { UserRole.Admin, "Administrator" },
            { UserRole.Supervisor, "Supervisor" },
            { UserRole.Supplier, "Supplier" }
        };

        // Public properties to expose for the dialog
        public bool ShouldUpdatePassword => IsNewUser || IsPasswordChangeRequested;
        public string FinalPassword => NewPassword;
    }
}