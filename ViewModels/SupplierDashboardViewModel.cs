using Microsoft.Extensions.Configuration;
using MiningOps.Models.Entities;
using MiningOps.Services;
using MiningOps.Utilities;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MiningOps.ViewModels
{
    public class SupplierDashboardViewModel : BaseViewModel
    {
        private readonly OrderService _orderService;
        private readonly InvoiceService _invoiceService;
        private readonly PaymentService _paymentService;
        private readonly SupplierService _supplierService;
        private readonly UserService _userService;
        private readonly IAuthenticationService _authService;

        public SupplierDashboardViewModel(IAuthenticationService authService, IConfiguration configuration, UserService userService)
        {
            _authService = authService;
            _userService = userService;

            _orderService = new OrderService(configuration);
            _invoiceService = new InvoiceService(configuration);
            _paymentService = new PaymentService(configuration);
            _supplierService = new SupplierService(configuration);

            // Initialize commands
            RefreshCommand = new RelayCommand(async () => await LoadDashboardDataAsync());
            ViewOrdersCommand = new RelayCommand(() => NavigateToOrders());
            ViewInvoicesCommand = new RelayCommand(() => NavigateToInvoices());
            ViewPaymentsCommand = new RelayCommand(() => NavigateToPayments());

            Debug.WriteLine($"✅ SupplierDashboardViewModel initialized for current user");
            _ = LoadDashboardDataAsync();
        }

        // === SUPPLIER-SPECIFIC METRICS ===

        // Order Metrics
        private int _totalOrders;
        public int TotalOrders
        {
            get => _totalOrders;
            set => SetProperty(ref _totalOrders, value);
        }

        private int _pendingOrders;
        public int PendingOrders
        {
            get => _pendingOrders;
            set => SetProperty(ref _pendingOrders, value);
        }

        private int _confirmedOrders;
        public int ConfirmedOrders
        {
            get => _confirmedOrders;
            set => SetProperty(ref _confirmedOrders, value);
        }

        private int _dispatchedOrders;
        public int DispatchedOrders
        {
            get => _dispatchedOrders;
            set => SetProperty(ref _dispatchedOrders, value);
        }

        private int _completedOrders;
        public int CompletedOrders
        {
            get => _completedOrders;
            set => SetProperty(ref _completedOrders, value);
        }

        private decimal _totalOrderValue;
        public decimal TotalOrderValue
        {
            get => _totalOrderValue;
            set => SetProperty(ref _totalOrderValue, value);
        }

        // Financial Metrics
        private decimal _totalInvoicedAmount;
        public decimal TotalInvoicedAmount
        {
            get => _totalInvoicedAmount;
            set => SetProperty(ref _totalInvoicedAmount, value);
        }

        private decimal _pendingPayments;
        public decimal PendingPayments
        {
            get => _pendingPayments;
            set => SetProperty(ref _pendingPayments, value);
        }

        private decimal _receivedPayments;
        public decimal ReceivedPayments
        {
            get => _receivedPayments;
            set => SetProperty(ref _receivedPayments, value);
        }

        private int _overdueInvoices;
        public int OverdueInvoices
        {
            get => _overdueInvoices;
            set => SetProperty(ref _overdueInvoices, value);
        }

        // Performance Metrics
        private string _completionRate;
        public string CompletionRate
        {
            get => _completionRate;
            set => SetProperty(ref _completionRate, value);
        }

        private string _averageOrderValue;
        public string AverageOrderValue
        {
            get => _averageOrderValue;
            set => SetProperty(ref _averageOrderValue, value);
        }

        private string _paymentCollectionRate;
        public string PaymentCollectionRate
        {
            get => _paymentCollectionRate;
            set => SetProperty(ref _paymentCollectionRate, value);
        }

        // Formatted Text Properties
        private string _overdueInvoicesText;
        public string OverdueInvoicesText
        {
            get => _overdueInvoicesText;
            set => SetProperty(ref _overdueInvoicesText, value);
        }

        private string _pendingPaymentsText;
        public string PendingPaymentsText
        {
            get => _pendingPaymentsText;
            set => SetProperty(ref _pendingPaymentsText, value);
        }

        // Recent Activity
        private ObservableCollection<PurchaseOrder> _recentOrders;
        public ObservableCollection<PurchaseOrder> RecentOrders
        {
            get => _recentOrders;
            set => SetProperty(ref _recentOrders, value);
        }

        private ObservableCollection<Invoice> _recentInvoices;
        public ObservableCollection<Invoice> RecentInvoices
        {
            get => _recentInvoices;
            set => SetProperty(ref _recentInvoices, value);
        }

        private ObservableCollection<Payment> _recentPayments;
        public ObservableCollection<Payment> RecentPayments
        {
            get => _recentPayments;
            set => SetProperty(ref _recentPayments, value);
        }

        // System Info
        private DateTime _lastRefreshTime;
        public DateTime LastRefreshTime
        {
            get => _lastRefreshTime;
            set => SetProperty(ref _lastRefreshTime, value);
        }

        private string _supplierName;
        public string SupplierName
        {
            get => _supplierName;
            set => SetProperty(ref _supplierName, value);
        }

        // === COMMANDS ===
        public ICommand RefreshCommand { get; }
        public ICommand ViewOrdersCommand { get; }
        public ICommand ViewInvoicesCommand { get; }
        public ICommand ViewPaymentsCommand { get; }

        // === DATA LOADING ===
        private async Task LoadDashboardDataAsync()
        {
            try
            {
                // Get supplier ID from current user
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                {
                    Debug.WriteLine("❌ No current user found");
                    MessageBox.Show("No user session found. Please log in again.",
                        "Session Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var supplierId = currentUser.AccId;
                Debug.WriteLine($"🔄 Loading supplier dashboard data for Supplier AccId: {supplierId}");

                // Load supplier profile using UserService
                var supplierProfile = await _userService.GetSupplierProfileAsync(supplierId);
                if (supplierProfile == null)
                {
                    Debug.WriteLine($"❌ No supplier profile found for AccId: {supplierId}");
                    MessageBox.Show("No supplier profile found for your account. Please contact administrator.",
                        "Supplier Profile Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Debug.WriteLine($"✅ Found supplier profile: {supplierProfile.CompanyName}");

                // Load supplier-specific data
                var orders = await SafeExecuteAsync(() => _orderService.GetOrdersBySupplierAsync(supplierProfile.SupplierId));
                var allInvoices = await SafeExecuteAsync(_invoiceService.GetAllInvoicesAsync);
                var allPayments = await SafeExecuteAsync(_paymentService.GetAllPaymentsAsync);

                // Filter invoices and payments for this supplier
                var supplierInvoices = allInvoices
                    .Where(i => i?.Order?.SupplierId == supplierProfile.SupplierId)
                    .ToList();

                var supplierPayments = allPayments
                    .Where(p => supplierInvoices.Any(i => i.InvoiceId == p.InvoiceId))
                    .ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        SupplierName = supplierProfile.CompanyName ?? "Supplier Portal";

                        // === ORDER CALCULATIONS ===
                        TotalOrders = orders.Count;
                        PendingOrders = orders.Count(o => o?.Status == OrderStatus.Pending);
                        ConfirmedOrders = orders.Count(o => o?.Status == OrderStatus.Confirmed);
                        DispatchedOrders = orders.Count(o => o?.Status == OrderStatus.Dispatched);
                        CompletedOrders = orders.Count(o => o?.Status == OrderStatus.Completed);
                        TotalOrderValue = orders.Where(o => o != null).Sum(o => o.TotalAmount);

                        // === FINANCIAL CALCULATIONS ===
                        TotalInvoicedAmount = supplierInvoices
                            .Where(i => i != null)
                            .Sum(i => SafeGetDecimal(i.Amount));

                        ReceivedPayments = supplierPayments
                            .Where(p => p != null)
                            .Sum(p => SafeGetDecimal(p.Amount));

                        PendingPayments = TotalInvoicedAmount - ReceivedPayments;
                        OverdueInvoices = supplierInvoices
                            .Count(i => i?.DueDate < DateTime.UtcNow &&
                                      (i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.Partial));

                        // === PERFORMANCE METRICS ===
                        // Completion Rate
                        CompletionRate = TotalOrders > 0 ?
                            $"{((double)CompletedOrders / TotalOrders * 100):0.0}%" : "0%";

                        // Average Order Value
                        AverageOrderValue = TotalOrders > 0 ?
                            (TotalOrderValue / TotalOrders).ToString("C2") : "$0.00";

                        // Payment Collection Rate
                        PaymentCollectionRate = TotalInvoicedAmount > 0 ?
                            $"{((decimal)ReceivedPayments / TotalInvoicedAmount * 100):0.0}%" : "0%";

                        // === FORMATTED TEXT ===
                        OverdueInvoicesText = $"{OverdueInvoices} overdue invoices";
                        PendingPaymentsText = $"Pending: {PendingPayments:C}";

                        // === RECENT ACTIVITY ===
                        RecentOrders = new ObservableCollection<PurchaseOrder>(
                            orders.Where(o => o != null)
                                  .OrderByDescending(o => o.CreatedAt)
                                  .Take(5)
                        );

                        RecentInvoices = new ObservableCollection<Invoice>(
                            supplierInvoices.Where(i => i != null)
                                          .OrderByDescending(i => i.InvoiceDate)
                                          .Take(5)
                        );

                        RecentPayments = new ObservableCollection<Payment>(
                            supplierPayments.Where(p => p != null)
                                          .OrderByDescending(p => p.PaidDate)
                                          .Take(5)
                        );

                        // === SYSTEM INFO ===
                        LastRefreshTime = DateTime.Now;

                        Debug.WriteLine($"✅ Supplier Dashboard Updated - Orders: {TotalOrders}, Invoiced: {TotalInvoicedAmount:C}, Received: {ReceivedPayments:C}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.WriteLine($"❌ Error in supplier UI update: {ex.Message}");
                        throw;
                    }
                });
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"💥 Critical error in supplier dashboard: {ex.Message}");
                MessageBox.Show($"Error loading supplier dashboard: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // === NAVIGATION METHODS ===
        private void NavigateToOrders()
        {
            MessageBox.Show("Navigating to Your Orders...", "Supplier Portal",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NavigateToInvoices()
        {
            MessageBox.Show("Navigating to Your Invoices...", "Supplier Portal",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NavigateToPayments()
        {
            MessageBox.Show("Navigating to Your Payments...", "Supplier Portal",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // === HELPER METHODS ===
        private decimal SafeGetDecimal(decimal? value)
        {
            return value ?? 0m;
        }

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
    }
}