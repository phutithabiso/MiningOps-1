using MiningOps.Models.Entities;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MiningOps.Dialogs
{
    public partial class PaymentProcessingDialog : Window, INotifyPropertyChanged
    {
        private Invoice _selectedInvoice;
        private decimal _amount;
        private string _paymentReference;
        private ObservableCollection<Invoice> _invoices;
        private string _approvedByName = "Current User";

        public Payment Payment { get; private set; }
        public ObservableCollection<Invoice> Invoices
        {
            get => _invoices;
            set => SetProperty(ref _invoices, value);
        }
        public Invoice SelectedInvoice
        {
            get => _selectedInvoice;
            set
            {
                _selectedInvoice = value;
                OnPropertyChanged();

                // AUTO-SET Amount when invoice is selected
                if (_selectedInvoice != null)
                {
                    // FIX: Use SafeAmount to handle nullable decimal
                    Amount = _selectedInvoice.SafeAmount;
                    OnPropertyChanged(nameof(Amount));

                    // Also update payment reference to include invoice reference
                    PaymentReference = $"PAY-{_selectedInvoice.SafeInvoiceReference}-{DateTime.UtcNow:HHmmss}";
                    OnPropertyChanged(nameof(PaymentReference));
                }
            }
        }

        public decimal Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        public string PaymentReference
        {
            get => _paymentReference;
            set => SetProperty(ref _paymentReference, value);
        }
        public string ApprovedByName
        {
            get => _approvedByName;
            set
            {
                _approvedByName = value;
                OnPropertyChanged();
            }
        }

        public PaymentProcessingDialog(ObservableCollection<Invoice> invoices)
        {
            InitializeComponent();
            DataContext = this;

            // FIX: Use SafeStatus and ensure we're filtering properly
            var payableInvoices = invoices?.Where(i =>
                i != null &&
                (i.SafeStatus == InvoiceStatus.Unpaid || i.SafeStatus == InvoiceStatus.Overdue)
            ).ToList() ?? new List<Invoice>();

            _invoices = new ObservableCollection<Invoice>(payableInvoices);
            Invoices = _invoices;

            Payment = new Payment
            {
                PaidDate = DateTime.UtcNow
            };

            // Auto-generate payment reference
            PaymentReference = $"PAY-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

            // Show info if no payable invoices
            if (!_invoices.Any())
            {
                MessageBox.Show("No unpaid or overdue invoices available for payment processing.",
                    "No Payable Invoices", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                Debug.WriteLine($"✅ PaymentDialog: Loaded {_invoices.Count} payable invoices");
                foreach (var invoice in _invoices)
                {
                    Debug.WriteLine($"   - Invoice {invoice.InvoiceId}: {invoice.SafeInvoiceReference}, Status: {invoice.SafeStatus}, Amount: {invoice.SafeAmount:C}");
                }
            }
        }

        private void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedInvoice == null)
            {
                MessageBox.Show("Please select an invoice", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Amount <= 0)
            {
                MessageBox.Show("Please enter a valid amount", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // FIX: Use SafeAmount instead of Amount for the validation
            if (Amount > SelectedInvoice.SafeAmount)
            {
                MessageBox.Show($"Payment amount cannot exceed invoice amount of {SelectedInvoice.SafeAmount:C}",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Update payment
            Payment.InvoiceId = SelectedInvoice.InvoiceId;
            Payment.Amount = Amount; // This is fine since Amount is non-nullable decimal
            Payment.PaymentReference = PaymentReference;

            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Add the missing SetProperty method for INotifyPropertyChanged
        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}