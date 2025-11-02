using MiningOps.Dialogs;
using MiningOps.Models.Entities;
using MiningOps.Utilities;
using MiningOps.Views;
using System.Windows;
using System.Windows.Input;

namespace MiningOps.ViewModels
{
    public class UserDetailsViewModel : BaseViewModel
    {
        private readonly RegisterMining _user;
        private readonly UserDetailsDialog _dialog;

        public RegisterMining User => _user;

        public ICommand EditUserCommand { get; }

        public UserDetailsViewModel(RegisterMining user, UserDetailsDialog dialog)
        {
            _user = user;
            _dialog = dialog;
            EditUserCommand = new RelayCommand(EditUser);
        }

        private void EditUser()
        {
            try
            {
                _dialog.DialogResult = false;
                _dialog.Close();

                // Open the edit user dialog
                var editDialog = new UserDialog(_user);
                if (editDialog.ShowDialog() == true)
                {
                    // User was edited - refresh might be handled by parent view
                    // Don't set UpdatedAt in the edit process
                    MessageBox.Show("User updated successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error editing user: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}