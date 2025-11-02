using System.Windows;
using System.Windows.Controls;
using MiningOps.ViewModels;

namespace MiningOps.Views
{
    public partial class UserProfileDialog : Window
    {
        public UserProfileViewModel ViewModel => (UserProfileViewModel)DataContext;

        public UserProfileDialog()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is UserProfileViewModel viewModel)
            {
                // Set up password box bindings
                CurrentPasswordBox.PasswordChanged += (s, args) =>
                {
                    viewModel.CurrentPassword = CurrentPasswordBox.Password;
                };

                NewPasswordBox.PasswordChanged += (s, args) =>
                {
                    viewModel.NewPassword = NewPasswordBox.Password;
                    viewModel.ValidatePasswordMatch();
                };

                ConfirmPasswordBox.PasswordChanged += (s, args) =>
                {
                    viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
                    viewModel.ValidatePasswordMatch();
                };
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is UserProfileViewModel viewModel)
            {
                if (viewModel.ValidateAndSave())
                {
                    DialogResult = true;
                    Close();
                }
            }
        }
    }
}