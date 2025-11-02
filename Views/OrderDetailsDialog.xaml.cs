using MiningOps.Models.Entities;
using System.Windows;

namespace MiningOps.Dialogs
{
    public partial class OrderDetailsDialog : Window
    {
        public OrderDetailsDialog(PurchaseOrder order)
        {
            InitializeComponent();
            DataContext = order;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}