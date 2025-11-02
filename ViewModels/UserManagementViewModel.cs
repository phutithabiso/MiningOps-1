using Microsoft.Extensions.Configuration;
using MiningOps.Dialogs;
using MiningOps.Models.Entities;
using MiningOps.Services;
using MiningOps.Utilities;
using MiningOps.Views;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MiningOps.ViewModels
{
    public class UserManagementViewModel : BaseViewModel
    {
        private readonly UserService _userService;
        private ObservableCollection<RegisterMining> _users;
        private RegisterMining _selectedUser;
        private int _totalUsersCount;
        private int _adminUsersCount;
        private int _supervisorUsersCount;
        private int _supplierUsersCount;

        public UserManagementViewModel(UserService userService)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            _userService = userService;
            Users = new ObservableCollection<RegisterMining>();

            // Initialize commands
            AddUserCommand = new RelayCommand(async () => await AddUserAsync());
            EditUserCommand = new RelayCommand<RegisterMining>(async (user) => await EditUserAsync(user));
            DeleteUserCommand = new RelayCommand<RegisterMining>(async (user) => await DeleteUserAsync(user));
            RefreshCommand = new RelayCommand(async () => await LoadUsersAsync());
            ShowUserDetailsCommand = new RelayCommand<RegisterMining>(ShowUserDetails);

            LoadUsersAsync();
        }

        public ObservableCollection<RegisterMining> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public RegisterMining SelectedUser
        {
            get => _selectedUser;
            set => SetProperty(ref _selectedUser, value);
        }

        // Dashboard properties
        public int TotalUsersCount
        {
            get => _totalUsersCount;
            private set => SetProperty(ref _totalUsersCount, value);
        }

        public int AdminUsersCount
        {
            get => _adminUsersCount;
            private set => SetProperty(ref _adminUsersCount, value);
        }

        public int SupervisorUsersCount
        {
            get => _supervisorUsersCount;
            private set => SetProperty(ref _supervisorUsersCount, value);
        }

        public int SupplierUsersCount
        {
            get => _supplierUsersCount;
            private set => SetProperty(ref _supplierUsersCount, value);
        }

        public ICommand AddUserCommand { get; }
        public ICommand EditUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ShowUserDetailsCommand { get; }

        private async Task LoadUsersAsync()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Users.Clear();
                    foreach (var user in users)
                        Users.Add(user);

                    // Update dashboard statistics
                    UpdateUserStatistics(users);
                });
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateUserStatistics(System.Collections.Generic.List<RegisterMining> users)
        {
            TotalUsersCount = users.Count;
            AdminUsersCount = users.Count(u => u.Role == UserRole.Admin);
            SupervisorUsersCount = users.Count(u => u.Role == UserRole.Supervisor);
            SupplierUsersCount = users.Count(u => u.Role == UserRole.Supplier);
        }

        private async Task AddUserAsync()
        {
            var dialog = new UserDialog();

            // Initialize with UserService for the dialog
            var viewModel = new UserDialogViewModel(_userService);
            dialog.DataContext = viewModel;

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Use the exposed properties from the ViewModel, not the dialog
                    await _userService.CreateUserAsync(viewModel.User, viewModel.FinalPassword);
                    await LoadUsersAsync();
                    MessageBox.Show("User added successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error adding user: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ShowUserDetails(RegisterMining user)
        {
            if (user != null)
            {
                // Pass user to constructor
                var detailsDialog = new UserDetailsDialog(user);
                detailsDialog.Owner = Application.Current.MainWindow;
                detailsDialog.ShowDialog();

                // Refresh data after dialog closes if user was edited
                if (detailsDialog.DialogResult == true)
                {
                    _ = LoadUsersAsync(); // Fire and forget - refresh data
                }
            }
        }

        private async Task EditUserAsync(RegisterMining user)
        {
            if (user == null)
            {
                MessageBox.Show("Please select a user to edit", "No User Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new UserDialog(user);

            // Initialize with UserService for the dialog
            var viewModel = new UserDialogViewModel(_userService, user);
            dialog.DataContext = viewModel;

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Update user properties from ViewModel
                    await _userService.UpdateUserAsync(viewModel.User);

                    // Update password if changed
                    if (viewModel.ShouldUpdatePassword)
                    {
                        await _userService.UpdateUserPasswordAsync(viewModel.User.AccId, viewModel.FinalPassword);
                    }

                    await LoadUsersAsync();
                    MessageBox.Show("User updated successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error updating user: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task DeleteUserAsync(RegisterMining user)
        {
            if (user == null)
            {
                MessageBox.Show("Please select a user to delete", "No User Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete user '{user.Username}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _userService.DeleteUserAsync(user.AccId);
                    await LoadUsersAsync();
                    MessageBox.Show("User deleted successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error deleting user: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}