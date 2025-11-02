// InventoryDetailsDialog.xaml.cs
using MiningOps.Models.Entities;
using System.Windows;

namespace MiningOps.Dialogs
{
    public partial class InventoryDetailsDialog : Window
    {
        public InventoryDetailsDialog(InventoryItem item)
        {
            InitializeComponent();
            DataContext = item;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}