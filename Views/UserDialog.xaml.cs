using MiningOps.Models.Entities;
using MiningOps.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace MiningOps.Dialogs
{
    public partial class UserDialog : Window
    {
        private UserDialogViewModel _viewModel;
        private RegisterMining _userToEdit;

        public UserDialog()
        {
            InitializeComponent();

            // Wait for DataContext to be set after initialization
            this.Loaded += OnWindowLoaded;
        }

        public UserDialog(Models.Entities.RegisterMining user) : this()
        {
            _userToEdit = user; // Store the user for later initialization
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _viewModel = DataContext as UserDialogViewModel;

            if (_viewModel != null)
            {
                // Set up the close event
                _viewModel.RequestClose += (result) =>
                {
                    this.DialogResult = result;
                    this.Close();
                };

                // If we have a user to edit, initialize the ViewModel
                if (_userToEdit != null)
                {
                    _viewModel.User = _userToEdit;
                    _viewModel.IsNewUser = false;
                }
            }
        }

        public RegisterMining User => _viewModel?.User;
        public string Password => _viewModel?.ShouldUpdatePassword == true ? _viewModel.FinalPassword : null;
        public bool IsPasswordChangeRequested => _viewModel?.IsPasswordChangeRequested ?? false;

        private void CurrentPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CurrentPassword = CurrentPasswordBox.Password;
            }
        }

        private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.NewPassword = NewPasswordBox.Password;
            }
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Let the ViewModel handle the save via command
            // The SaveCommand is already bound to the Save button
        }

        // Add this method to handle the password change checkbox
        private void ChangePasswordCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && sender is CheckBox checkBox)
            {
                _viewModel.IsPasswordChangeRequested = checkBox.IsChecked == true;
            }
        }

        // Also handle the Unchecked event
        private void ChangePasswordCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null && sender is CheckBox checkBox)
            {
                _viewModel.IsPasswordChangeRequested = checkBox.IsChecked == true;
            }
        }
    }
}