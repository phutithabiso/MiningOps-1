using Microsoft.Extensions.Configuration;
using MiningOps.Dialogs;
using MiningOps.Models.Entities;
using MiningOps.Services;
using MiningOps.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MiningOps.ViewModels
{
    public class PaymentProcessingViewModel : BaseViewModel
    {
        private readonly PaymentService _paymentService;
        private readonly InvoiceService _invoiceService;
        private readonly OrderService _orderService;
        private ObservableCollection<Payment> _payments;
        private ObservableCollection<Invoice> _invoices;
        private ObservableCollection<PurchaseOrder> _orders;
        private Payment _selectedPayment;
        private string _searchText;
        private string _statusFilter = "All";
        private bool _isLoading;

        // Dashboard Properties
        private ObservableCollection<Payment> _recentPayments;
        private ObservableCollection<Payment> _pendingApprovalPayments;
        private int _totalPaymentsCount;
        private int _approvedPaymentsCount;
        private int _pendingApprovalCount;
        private decimal _averagePaymentAmount;
        private decimal _highestPaymentAmount;

        public PaymentProcessingViewModel(PaymentService paymentService, InvoiceService invoiceService, OrderService orderService)
        {
           /* var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();*/

            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(paymentService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(paymentService));

            Payments = new ObservableCollection<Payment>();
            Invoices = new ObservableCollection<Invoice>();
            Orders = new ObservableCollection<PurchaseOrder>();
            RecentPayments = new ObservableCollection<Payment>();
            PendingApprovalPayments = new ObservableCollection<Payment>();

            // Initialize commands
            ProcessPaymentCommand = new RelayCommand(async () => await ProcessPaymentAsync());
            ViewPaymentDetailsCommand = new RelayCommand<Payment>(async (payment) => await ViewPaymentDetailsAsync(payment));
            ApprovePaymentCommand = new RelayCommand<Payment>(async (payment) => await ApprovePaymentAsync(payment));
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            SearchCommand = new RelayCommand(async () => await SearchPaymentsAsync());
            ExportPaymentsCommand = new RelayCommand(async () => await ExportPaymentsAsync());
            CreateInvoiceCommand = new RelayCommand<PurchaseOrder>(async (order) => await CreateInvoiceAsync(order));

            // Load data with loading state
            _ = LoadDataAsync();
        }

        // Main Collections
        public ObservableCollection<Payment> Payments
        {
            get => _payments;
            set => SetProperty(ref _payments, value);
        }

        public ObservableCollection<Invoice> Invoices
        {
            get => _invoices;
            set => SetProperty(ref _invoices, value);
        }

        public ObservableCollection<PurchaseOrder> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }

        public Payment SelectedPayment
        {
            get => _selectedPayment;
            set => SetProperty(ref _selectedPayment, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set => SetProperty(ref _statusFilter, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // Dashboard Properties
        public ObservableCollection<Payment> RecentPayments
        {
            get => _recentPayments;
            set => SetProperty(ref _recentPayments, value);
        }

        public ObservableCollection<Payment> PendingApprovalPayments
        {
            get => _pendingApprovalPayments;
            set => SetProperty(ref _pendingApprovalPayments, value);
        }

        public int TotalPaymentsCount
        {
            get => _totalPaymentsCount;
            set => SetProperty(ref _totalPaymentsCount, value);
        }

        public int ApprovedPaymentsCount
        {
            get => _approvedPaymentsCount;
            set => SetProperty(ref _approvedPaymentsCount, value);
        }

        public int PendingApprovalCount
        {
            get => _pendingApprovalCount;
            set => SetProperty(ref _pendingApprovalCount, value);
        }

        public decimal AveragePaymentAmount
        {
            get => _averagePaymentAmount;
            set => SetProperty(ref _averagePaymentAmount, value);
        }

        public decimal HighestPaymentAmount
        {
            get => _highestPaymentAmount;
            set => SetProperty(ref _highestPaymentAmount, value);
        }

        // FIXED: Financial summary properties using safe properties
        public decimal TotalOutstanding
        {
            get
            {
                try
                {
                    return Invoices?
                        .Where(i => i != null && (i.SafeStatus == InvoiceStatus.Unpaid || i.SafeStatus == InvoiceStatus.Overdue))
                        .Sum(i => i.SafeAmount) ?? 0m;
                }
                catch
                {
                    return 0m;
                }
            }
        }

        public decimal TotalPaidThisMonth
        {
            get
            {
                try
                {
                    var currentMonth = DateTime.Now.Month;
                    var currentYear = DateTime.Now.Year;
                    return Payments?
                        .Where(p => p != null && p.PaidDate.Month == currentMonth && p.PaidDate.Year == currentYear)
                        .Sum(p => p.SafeAmount) ?? 0m;
                }
                catch
                {
                    return 0m;
                }
            }
        }

        public int OverdueInvoicesCount
        {
            get
            {
                try
                {
                    return Invoices?
                        .Count(i => i != null && i.SafeStatus == InvoiceStatus.Overdue) ?? 0;
                }
                catch
                {
                    return 0;
                }
            }
        }

        public int UnpaidInvoicesCount
        {
            get
            {
                try
                {
                    return Invoices?
                        .Count(i => i != null && i.SafeStatus == InvoiceStatus.Unpaid) ?? 0;
                }
                catch
                {
                    return 0;
                }
            }
        }

        // Computed Properties for Dashboard
        public string ApprovalRate => TotalPaymentsCount > 0 ?
            $"{((double)ApprovedPaymentsCount / TotalPaymentsCount * 100):0.0}%" : "0%";

        public string MonthlyTrend
        {
            get
            {
                var lastMonth = DateTime.Now.AddMonths(-1);
                var lastMonthPayments = Payments?
                    .Where(p => p != null && p.PaidDate.Month == lastMonth.Month && p.PaidDate.Year == lastMonth.Year)
                    .Sum(p => p.SafeAmount) ?? 0m;

                if (lastMonthPayments == 0) return "N/A";

                var change = ((TotalPaidThisMonth - lastMonthPayments) / lastMonthPayments * 100);
                return change >= 0 ? $"+{change:0.0}%" : $"{change:0.0}%";
            }
        }

        public ICommand ProcessPaymentCommand { get; }
        public ICommand ViewPaymentDetailsCommand { get; }
        public ICommand ApprovePaymentCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ExportPaymentsCommand { get; }
        public ICommand CreateInvoiceCommand { get; }

        private async Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                // Clear existing data
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Payments.Clear();
                    Invoices.Clear();
                    Orders.Clear();
                    RecentPayments.Clear();
                    PendingApprovalPayments.Clear();
                });

                List<Payment> payments = null;
                List<Invoice> invoices = null;
                List<PurchaseOrder> orders = null;

                // Load data in parallel for better performance
                var paymentTask = _paymentService.GetAllPaymentsAsync();
                var invoiceTask = _invoiceService.GetAllInvoicesAsync();
                var orderTask = _orderService.GetAllOrdersAsync();

                await Task.WhenAll(paymentTask, invoiceTask, orderTask);

                payments = await paymentTask;
                invoices = await invoiceTask;
                orders = await orderTask;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var payment in payments.Where(p => p != null))
                        Payments.Add(payment);

                    foreach (var invoice in invoices.Where(i => i != null))
                        Invoices.Add(invoice);

                    foreach (var order in orders.Where(o => o != null))
                        Orders.Add(order);

                    // Calculate dashboard metrics
                    CalculatePaymentMetrics(payments, invoices);
                });

                // Debug: Show invoice statuses
                Debug.WriteLine("📋 INVOICE STATUS REPORT:");
                foreach (var invoice in invoices)
                {
                    Debug.WriteLine($"   - Invoice {invoice.InvoiceId}: {invoice.SafeInvoiceReference}, " +
                                  $"DB Status: {invoice.Status}, Safe Status: {invoice.SafeStatus}, " +
                                  $"Amount: {invoice.SafeAmount:C}");
                }

                // Debug: Show payment statuses
                Debug.WriteLine("💰 PAYMENT STATUS REPORT:");
                foreach (var payment in payments.Take(10)) // Show first 10
                {
                    Debug.WriteLine($"   - Payment {payment.PaymentId}: Invoice {payment.InvoiceId}, " +
                                  $"Amount: {payment.SafeAmount:C}, Approved: {payment.ApprovedById}");
                }

                // Update financial summaries
                OnPropertyChanged(nameof(TotalOutstanding));
                OnPropertyChanged(nameof(TotalPaidThisMonth));
                OnPropertyChanged(nameof(OverdueInvoicesCount));
                OnPropertyChanged(nameof(UnpaidInvoicesCount));
                OnPropertyChanged(nameof(ApprovalRate));
                OnPropertyChanged(nameof(MonthlyTrend));

                Debug.WriteLine($"📊 PaymentProcessingViewModel - Invoices: {invoices.Count}, Payments: {payments.Count}, Orders: {orders.Count}");
                Debug.WriteLine($"💰 Financial Summary - Outstanding: {TotalOutstanding:C}, Paid This Month: {TotalPaidThisMonth:C}");

            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"💥 Error loading payment data: {ex.Message}");
                MessageBox.Show($"Error loading payment data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CalculatePaymentMetrics(List<Payment> payments, List<Invoice> invoices)
        {
            try
            {
                // Basic payment statistics
                TotalPaymentsCount = payments.Count;
                ApprovedPaymentsCount = payments.Count(p => p.ApprovedById != null);
                PendingApprovalCount = payments.Count(p => p.ApprovedById == null);

                // Amount calculations
                var validPayments = payments.Where(p => p != null).ToList();
                AveragePaymentAmount = validPayments.Any() ?
                    validPayments.Average(p => p.SafeAmount) : 0m;
                HighestPaymentAmount = validPayments.Any() ?
                    validPayments.Max(p => p.SafeAmount) : 0m;

                // Recent payments (last 5 by paid date)
                var recentPayments = validPayments
                    .OrderByDescending(p => p.PaidDate)
                    .Take(5)
                    .ToList();

                RecentPayments.Clear();
                foreach (var payment in recentPayments)
                    RecentPayments.Add(payment);

                // Pending approval payments (where ApprovedById is null)
                var pendingApproval = validPayments
                    .Where(p => p.ApprovedById == null)
                    .Take(5)
                    .ToList();

                PendingApprovalPayments.Clear();
                foreach (var payment in pendingApproval)
                    PendingApprovalPayments.Add(payment);

                Debug.WriteLine($"📊 Payment Metrics - Total: {TotalPaymentsCount}, Approved: {ApprovedPaymentsCount}, Pending: {PendingApprovalCount}");
                Debug.WriteLine($"💰 Amount Stats - Average: {AveragePaymentAmount:C}, Highest: {HighestPaymentAmount:C}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error calculating payment metrics: {ex.Message}");
            }
        }

        private async Task DebugPaymentApproval()
        {
            try
            {
                var payments = await _paymentService.GetAllPaymentsAsync();
                Debug.WriteLine($"🔍 Found {payments.Count} total payments");

                foreach (var p in payments.Take(5))
                {
                    Debug.WriteLine($"   - Payment {p.PaymentId}: Amount={p.SafeAmount:C}, ApprovedBy={p.ApprovedById}, Reference={p.SafePaymentReference}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"🔍 Debug failed: {ex.Message}");
            }
        }

        private async Task DebugDataLoading()
        {
            try
            {
                // Test service data loading instead of direct DB access
                var invoiceService = new InvoiceService(new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json").Build());

                var invoices = await invoiceService.GetAllInvoicesAsync();
                Debug.WriteLine($"🔍 Service Loading - Invoices: {invoices.Count}");

                foreach (var invoice in invoices.Take(3))
                {
                    Debug.WriteLine($"   - Invoice {invoice.InvoiceId}: {invoice.SafeInvoiceReference}, Amount: {invoice.SafeAmount:C}, Status: {invoice.SafeStatus}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"🔍 Debug Data Loading Failed: {ex.Message}");
            }
        }

        private async Task SearchPaymentsAsync()
        {
            try
            {
                var allPayments = await _paymentService.GetAllPaymentsAsync() ?? new List<Payment>();
                var filteredPayments = allPayments.Where(payment =>
                    (StatusFilter == "All" || GetPaymentStatus(payment) == StatusFilter) &&
                    (string.IsNullOrEmpty(SearchText) ||
                     payment.PaymentId.ToString().Contains(SearchText) ||
                     payment.Invoice?.SafeInvoiceReference?.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) == true ||
                     payment.SafePaymentReference?.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) == true)
                ).ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Payments.Clear();
                    foreach (var payment in filteredPayments)
                        Payments.Add(payment);
                });

                Debug.WriteLine($"🔍 Payment Search: {filteredPayments.Count} payments match filter '{StatusFilter}' and search '{SearchText}'");
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"❌ Error searching payments: {ex.Message}");
                MessageBox.Show($"Error searching payments: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetPaymentStatus(Payment payment)
        {
            if (payment.ApprovedById == null)
                return "Pending";

            return "Approved";
        }

        private async Task CreateInvoiceAsync(PurchaseOrder order)
        {
            if (order == null)
            {
                MessageBox.Show("Please select an order to create invoice", "No Order Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var invoice = await _orderService.CreateInvoiceFromOrderAsync(order.OrderId);
                await LoadDataAsync(); // Refresh data after creating invoice
                MessageBox.Show($"Invoice created successfully: {invoice.InvoiceReference}", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"❌ Error creating invoice: {ex.Message}");
                MessageBox.Show($"Error creating invoice: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ProcessPaymentAsync()
        {
            // Check if we have invoices
            if (!Invoices.Any(i => i.SafeStatus == InvoiceStatus.Unpaid || i.SafeStatus == InvoiceStatus.Overdue))
            {
                MessageBox.Show("No unpaid or overdue invoices available. Please create invoices first by confirming orders.", "No Payable Invoices",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Filter to only unpaid/overdue invoices for the dialog
            var payableInvoices = new ObservableCollection<Invoice>(
                Invoices.Where(i => i.SafeStatus == InvoiceStatus.Unpaid || i.SafeStatus == InvoiceStatus.Overdue)
            );

            var dialog = new PaymentProcessingDialog(payableInvoices);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var payment = await _paymentService.CreatePaymentAsync(dialog.Payment);

                    // FIXED: Now using the new UpdateInvoiceStatusAsync method
                    await _invoiceService.UpdateInvoiceStatusAsync(payment.InvoiceId, InvoiceStatus.Paid);

                    await LoadDataAsync();
                    MessageBox.Show("Payment processed successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    Debug.WriteLine($"✅ Processed payment ID: {payment.PaymentId}");
                }
                catch (System.Exception ex)
                {
                    Debug.WriteLine($"❌ Error processing payment: {ex.Message}");
                    MessageBox.Show($"Error processing payment: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ViewPaymentDetailsAsync(Payment payment)
        {
            if (payment == null)
            {
                MessageBox.Show("Please select a payment to view details", "No Payment Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var detailsDialog = new PaymentDetailsDialog(payment);
            detailsDialog.ShowDialog();
        }

        private async Task ApprovePaymentAsync(Payment payment)
        {
            if (payment == null)
            {
                MessageBox.Show("Please select a payment to approve", "No Payment Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to approve payment #{payment.PaymentId} for {payment.SafeAmount:C}?",
                "Confirm Approval", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // For demo, using current user ID - you should replace with actual logged-in user ID
                    int currentUserId = 1; // This should come from your authentication system

                    Debug.WriteLine($"🔍 Attempting to approve payment {payment.PaymentId} by user {currentUserId}");

                    // Call the service method
                    bool success = await _paymentService.ApprovePaymentAsync(payment.PaymentId, currentUserId);

                    if (success)
                    {
                        Debug.WriteLine($"✅ Payment {payment.PaymentId} approved successfully");

                        // Refresh the data to show the updated approval
                        await LoadDataAsync();

                        // Show specific success message
                        MessageBox.Show($"Payment #{payment.PaymentId} has been approved successfully!",
                            "Approval Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        Debug.WriteLine($"❌ Payment {payment.PaymentId} approval failed");
                        MessageBox.Show($"Failed to approve payment #{payment.PaymentId}. The payment may not exist or is already approved.",
                            "Approval Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.WriteLine($"💥 Error approving payment: {ex.Message}");
                    MessageBox.Show($"Error approving payment: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ExportPaymentsAsync()
        {
            try
            {
                var exportData = Payments.Select(p => new
                {
                    p.PaymentId,
                    p.SafePaymentReference,
                    p.SafeAmount,
                    p.PaidDate,
                    InvoiceReference = p.Invoice?.SafeInvoiceReference,
                    Status = p.ApprovedById == null ? "Pending" : "Approved",
                    ApprovedBy = p.ApprovedBy?.FullName ?? "Not Approved"
                }).ToList();

                // Here you would implement actual export logic (CSV, Excel, PDF)
                MessageBox.Show($"Ready to export {exportData.Count} payments. Export functionality would be implemented here.", "Export Feature",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                Debug.WriteLine($"📤 Export prepared for {exportData.Count} payments");
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"❌ Error exporting payments: {ex.Message}");
                MessageBox.Show($"Error exporting payments: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Helper method to safely execute service calls
        private async Task<List<T>> SafeExecuteAsync<T>(Func<Task<List<T>>> serviceCall)
        {
            try
            {
                var result = await serviceCall();
                return result ?? new List<T>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Service call failed: {ex.Message}");
                return new List<T>();
            }
        }

        // Helper method to safely get decimal values from nullable types
        private decimal SafeGetDecimal(decimal? value)
        {
            return value ?? 0m;
        }

        // Helper method to safely get int values from nullable types
        private int SafeGetInt(int? value)
        {
            return value ?? 0;
        }
    }
}