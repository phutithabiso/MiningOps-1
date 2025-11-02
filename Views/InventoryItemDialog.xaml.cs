using MiningOps.Models.Entities;
using MiningOps.ViewModels.Dialogs;
using System.Collections.Generic;
using System.Windows;

namespace MiningOps.Dialogs
{
    public partial class InventoryItemDialog : Window
    {
        public InventoryItemDialogViewModel ViewModel { get; set; }

        public InventoryItem InventoryItem => ViewModel?.InventoryItem;

        public InventoryItemDialog(List<Warehouse> warehouses)
        {
            InitializeComponent();
            ViewModel = new InventoryItemDialogViewModel(warehouses);
            DataContext = ViewModel;
        }

        public InventoryItemDialog(List<Warehouse> warehouses, InventoryItem existingItem)
        {
            InitializeComponent();
            ViewModel = new InventoryItemDialogViewModel(warehouses, existingItem);
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
                MessageBox.Show("Please fill in all required fields with valid values.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}