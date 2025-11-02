// Views/LoginView.xaml.cs
using System.Windows;
using System.Windows.Controls;
using MiningOps.ViewModels;

namespace MiningOps.Views
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                // Handle password box binding
                PasswordBox.PasswordChanged += (s, args) =>
                {
                    viewModel.Password = PasswordBox.Password;
                };

                // Clear password box when error is cleared
                viewModel.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(LoginViewModel.ErrorMessage) &&
                        string.IsNullOrEmpty(viewModel.ErrorMessage))
                    {
                        PasswordBox.Password = string.Empty;
                        viewModel.Password = string.Empty;
                    }
                };
            }
        }
    }
}