using MiningOps.Models.Entities;
using System.Windows;

namespace MiningOps.Dialogs
{
    public partial class PaymentDetailsDialog : Window
    {
        public PaymentDetailsDialog(Payment payment)
        {
            InitializeComponent();
            DataContext = payment;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}