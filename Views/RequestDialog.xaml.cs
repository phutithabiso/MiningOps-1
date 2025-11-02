using MiningOps.Models.Entities;
using MiningOps.ViewModels.Dialogs;
using System.Windows;

namespace MiningOps.Dialogs
{
    public partial class RequestDialog : Window
    {
        public RequestDialogViewModel ViewModel { get; set; }

        public MaterialRequest Request => ViewModel?.Request;

        public RequestDialog()
        {
            InitializeComponent();
            ViewModel = new RequestDialogViewModel();
            DataContext = ViewModel;
        }

        public RequestDialog(MaterialRequest existingRequest)
        {
            InitializeComponent();
            ViewModel = new RequestDialogViewModel(existingRequest);
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
                MessageBox.Show("Please fill in all required fields and ensure quantity is greater than 0.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}