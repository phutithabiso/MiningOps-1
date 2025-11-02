using MiningOps.Models.Entities;
using MiningOps.ViewModels;
using System.Windows;

namespace MiningOps.Views
{
    public partial class UserDetailsDialog : Window
    {
        public UserDetailsDialog(RegisterMining user)
        {
            InitializeComponent();
            DataContext = new UserDetailsViewModel(user, this);
        }

        // Parameterless constructor for design time
        public UserDetailsDialog() : this(new RegisterMining())
        {
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}