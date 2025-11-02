using MiningOps.Models.Entities;
using MiningOps.Services;
using MiningOps.ViewModels.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace MiningOps.Dialogs
{
    public partial class SupplierDialog : Window
    {
        public SupplierDialogViewModel ViewModel { get; set; }

        public Supplier Supplier => ViewModel?.Supplier;

        public SupplierDialog(UserService userService)
        {
            InitializeComponent();
            ViewModel = new SupplierDialogViewModel(userService);
            DataContext = ViewModel;
        }

        public SupplierDialog(UserService userService, Supplier existingSupplier)
        {
            InitializeComponent();
            ViewModel = new SupplierDialogViewModel(userService, existingSupplier);
            DataContext = ViewModel;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.CanSave())
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please fill in all required fields.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}