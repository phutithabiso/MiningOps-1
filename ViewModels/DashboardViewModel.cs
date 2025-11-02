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
using System.Windows.Media;

namespace MiningOps.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService; // Add this
        private readonly UserService _userService;
        private readonly InventoryService _inventoryService;
        private readonly SupplierService _supplierService;
        private readonly WarehouseService _warehouseService;
        private readonly RequestService _requestService;
        private readonly OrderService _orderService;
        private readonly PaymentService _paymentService;
        private readonly InvoiceService _invoiceService;

        public DashboardViewModel(
            IAuthenticationService authService,
       UserService userService,
       InventoryService inventoryService,
       SupplierService supplierService,
       WarehouseService warehouseService,
       RequestService requestService,
       OrderService orderService,
       PaymentService paymentService,
       InvoiceService invoiceService)
        {
            _userService = userService;
            _inventoryService = inventoryService;
            _supplierService = supplierService;
            _warehouseService = warehouseService;
            _requestService = requestService;
            _orderService = orderService;
            _paymentService = paymentService;
            _invoiceService = invoiceService;
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));

            /*  var configuration = new ConfigurationBuilder()
                  .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                  .Build();

              _userService = new UserService(configuration);
              _inventoryService = new InventoryService(configuration);
              _supplierService = new SupplierService(configuration);
              _warehouseService = new WarehouseService(configuration);
              _requestService = new RequestService(configuration);
              _orderService = new OrderService(configuration);
              _paymentService = new PaymentService(configuration);
              _invoiceService = new InvoiceService(configuration);*/
            InitializeRoleBasedDashboard();
            // Initialize commands
            RefreshCommand = new RelayCommand(async () => await LoadDashboardDataAsync());
            CreateRequestCommand = new RelayCommand(() => NavigateToRequests());
            ManageInventoryCommand = new RelayCommand(() => NavigateToInventory());
            ViewOrdersCommand = new RelayCommand(() => NavigateToOrders());
            ProcessPaymentsCommand = new RelayCommand(() => NavigateToPayments());
            ManageUsersCommand = new RelayCommand(() => NavigateToUsers());

            Debug.WriteLine("✅ DashboardViewModel initialized");
            _ = LoadDashboardDataAsync();
            _ = TestDatabaseAndEntities();
        }

        // === EXISTING PROPERTIES (keep these) ===
        private string _dashboardTitle;
        public string DashboardTitle
        {
            get => _dashboardTitle;
            set => SetProperty(ref _dashboardTitle, value);
        }
        private string _welcomeMessage;
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }
        private bool _showAdminMetrics = true;
        public bool ShowAdminMetrics
        {
            get => _showAdminMetrics;
            set => SetProperty(ref _showAdminMetrics, value);
        }

        private bool _showSupervisorMetrics = true;
        public bool ShowSupervisorMetrics
        {
            get => _showSupervisorMetrics;
            set => SetProperty(ref _showSupervisorMetrics, value);
        }

        private bool _showSupplierMetrics = true;
        public bool ShowSupplierMetrics
        {
            get => _showSupplierMetrics;
            set => SetProperty(ref _showSupplierMetrics, value);
        }
        private int _totalUsers;
        public int TotalUsers
        {
            get => _totalUsers;
            set => SetProperty(ref _totalUsers, value);
        }

        private int _totalInventoryItems;
        public int TotalInventoryItems
        {
            get => _totalInventoryItems;
            set => SetProperty(ref _totalInventoryItems, value);
        }

        private int _totalSuppliers;
        public int TotalSuppliers
        {
            get => _totalSuppliers;
            set => SetProperty(ref _totalSuppliers, value);
        }

        private int _totalWarehouses;
        public int TotalWarehouses
        {
            get => _totalWarehouses;
            set => SetProperty(ref _totalWarehouses, value);
        }

        private int _pendingRequests;
        public int PendingRequests
        {
            get => _pendingRequests;
            set => SetProperty(ref _pendingRequests, value);
        }

        private int _activeOrders;
        public int ActiveOrders
        {
            get => _activeOrders;
            set => SetProperty(ref _activeOrders, value);
        }

        private decimal _totalPayments;
        public decimal TotalPayments
        {
            get => _totalPayments;
            set => SetProperty(ref _totalPayments, value);
        }

        private decimal _totalInventoryValue;
        public decimal TotalInventoryValue
        {
            get => _totalInventoryValue;
            set => SetProperty(ref _totalInventoryValue, value);
        }

        private int _overdueInvoices;
        public int OverdueInvoices
        {
            get => _overdueInvoices;
            set => SetProperty(ref _overdueInvoices, value);
        }

        private decimal _outstandingInvoices;
        public decimal OutstandingInvoices
        {
            get => _outstandingInvoices;
            set => SetProperty(ref _outstandingInvoices, value);
        }

        private ObservableCollection<InventoryItem> _lowStockItems;
        public ObservableCollection<InventoryItem> LowStockItems
        {
            get => _lowStockItems;
            set => SetProperty(ref _lowStockItems, value);
        }

        private ObservableCollection<MaterialRequest> _recentRequests;
        public ObservableCollection<MaterialRequest> RecentRequests
        {
            get => _recentRequests;
            set => SetProperty(ref _recentRequests, value);
        }

        private ObservableCollection<Invoice> _recentInvoices;
        public ObservableCollection<Invoice> RecentInvoices
        {
            get => _recentInvoices;
            set => SetProperty(ref _recentInvoices, value);
        }

        // === NEW PROPERTIES FOR ENHANCED DASHBOARD ===
        private string _overdueInvoicesText;
        public string OverdueInvoicesText
        {
            get => _overdueInvoicesText;
            set => SetProperty(ref _overdueInvoicesText, value);
        }

        private string _outstandingInvoicesText;
        public string OutstandingInvoicesText
        {
            get => _outstandingInvoicesText;
            set => SetProperty(ref _outstandingInvoicesText, value);
        }

        private string _completionRate;
        public string CompletionRate
        {
            get => _completionRate;
            set => SetProperty(ref _completionRate, value);
        }

        private string _approvalRate;
        public string ApprovalRate
        {
            get => _approvalRate;
            set => SetProperty(ref _approvalRate, value);
        }

        private string _monthlyTrend;
        public string MonthlyTrend
        {
            get => _monthlyTrend;
            set => SetProperty(ref _monthlyTrend, value);
        }

        private Brush _trendColor;
        public Brush TrendColor
        {
            get => _trendColor;
            set => SetProperty(ref _trendColor, value);
        }

        private DateTime _lastRefreshTime;
        public DateTime LastRefreshTime
        {
            get => _lastRefreshTime;
            set => SetProperty(ref _lastRefreshTime, value);
        }

        private int _activeUsers;
        public int ActiveUsers
        {
            get => _activeUsers;
            set => SetProperty(ref _activeUsers, value);
        }
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

        private decimal _totalOrderValue;
        public decimal TotalOrderValue
        {
            get => _totalOrderValue;
            set => SetProperty(ref _totalOrderValue, value);
        }

        // === COMMANDS ===
        public ICommand RefreshCommand { get; }
        public ICommand CreateRequestCommand { get; }
        public ICommand ManageInventoryCommand { get; }
        public ICommand ViewOrdersCommand { get; }
        public ICommand ProcessPaymentsCommand { get; }
        public ICommand ManageUsersCommand { get; }

        // === EXISTING METHODS (keep these) ===

        private void InitializeRoleBasedDashboard()
        {
            var currentUser = _authService.CurrentUser;
            if (currentUser != null)
            {
                switch (currentUser.Role)
                {
                    case UserRole.Admin:
                        DashboardTitle = "Administrator Dashboard";
                        WelcomeMessage = $"Welcome, System Administrator";
                        ShowAdminMetrics = true;
                        ShowSupervisorMetrics = true;
                        ShowSupplierMetrics = true;
                        break;

                    case UserRole.Supervisor:
                        DashboardTitle = "Supervisor Dashboard";
                        WelcomeMessage = $"Welcome, {currentUser.FullName}";
                        ShowAdminMetrics = false; // Hide admin-specific metrics
                        ShowSupervisorMetrics = true;
                        ShowSupplierMetrics = false;
                        break;

                    case UserRole.Supplier:
                        DashboardTitle = "Supplier Portal";
                        WelcomeMessage = $"Welcome, {currentUser.FullName}";
                        ShowAdminMetrics = false;
                        ShowSupervisorMetrics = false;
                        ShowSupplierMetrics = true;
                        break;
                }
            }
            else
            {
                DashboardTitle = "Dashboard";
                WelcomeMessage = "Welcome";
            }

            Debug.WriteLine($"🎯 Dashboard initialized for: {DashboardTitle}");
        }
        private async Task TestDatabaseAndEntities()
        {
            try
            {
                Debug.WriteLine("🔍 Testing database and entities...");

                // Test database connection
                var dbService = new DatabaseService(new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json").Build());

                var connectionTest = await dbService.TestConnectionAsync();
                Debug.WriteLine($"Database connection: {(connectionTest ? "✅ SUCCESS" : "❌ FAILED")}");

                if (connectionTest)
                {
                    // Test individual entity loading
                    await TestEntityLoading();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 Database test failed: {ex.Message}");
            }
        }

        private async Task TestEntityLoading()
        {
            try
            {
                Debug.WriteLine("🧪 Testing entity loading...");

                var users = await _userService.GetAllUsersAsync();
                Debug.WriteLine($"Users loaded: {users?.Count ?? -1}");

                var inventory = await _inventoryService.GetAllItemsAsync();
                Debug.WriteLine($"Inventory items loaded: {inventory?.Count ?? -1}");

                var suppliers = await _supplierService.GetAllSuppliersAsync();
                Debug.WriteLine($"Suppliers loaded: {suppliers?.Count ?? -1}");

                var warehouses = await _warehouseService.GetAllWarehousesAsync();
                Debug.WriteLine($"Warehouses loaded: {warehouses?.Count ?? -1}");

                Debug.WriteLine("✅ Entity loading test completed");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Entity loading test failed: {ex.Message}");
                Debug.WriteLine($"Full error: {ex}");
            }
        }

        // === UPDATED LoadDashboardDataAsync METHOD ===
        private async Task LoadDashboardDataAsync()
        {
            try
            {
                Debug.WriteLine("🔄 Starting dashboard data load...");

                // --- Common data (shared across most roles) ---
                var inventoryItems = await SafeExecuteAsync(_inventoryService.GetAllItemsAsync);
                var requests = await SafeExecuteAsync(_requestService.GetAllRequestsAsync);
                var orders = await SafeExecuteAsync(_orderService.GetAllOrdersAsync);

                // --- ADMIN METRICS ---
                if (ShowAdminMetrics)
                {
                    var users = await SafeExecuteAsync(_userService.GetAllUsersAsync);
                    var suppliers = await SafeExecuteAsync(_supplierService.GetAllSuppliersAsync);
                    var warehouses = await SafeExecuteAsync(_warehouseService.GetAllWarehousesAsync);
                    var payments = await SafeExecuteAsync(_paymentService.GetAllPaymentsAsync);
                    var invoices = await SafeExecuteAsync(_invoiceService.GetAllInvoicesAsync);
                    var overdueInvoices = await SafeExecuteAsync(_invoiceService.GetOverdueInvoicesAsync);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TotalUsers = users.Count;
                        TotalSuppliers = suppliers.Count;
                        TotalWarehouses = warehouses.Count;

                        TotalPayments = payments
                            .Where(p => p != null)
                            .Sum(p => SafeGetDecimal(p.Amount));

                        OverdueInvoices = overdueInvoices.Count;

                        OutstandingInvoices = invoices
                            .Where(i => i != null && (i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.Overdue))
                            .Sum(i => SafeGetDecimal(i.Amount));
                    });
                }

                // --- SUPERVISOR METRICS ---
                if (ShowSupervisorMetrics)
                {
                    var lowStockItems = await SafeExecuteAsync(_inventoryService.GetLowStockItemsAsync);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TotalInventoryItems = inventoryItems.Count;

                        TotalInventoryValue = inventoryItems
                            .Where(i => i != null)
                            .Sum(i => SafeGetDecimal(i.Quantity) * SafeGetDecimal(i.UnitCost));

                        PendingRequests = requests.Count(r => r?.Status == "Pending");
                        ActiveOrders = orders.Count(o => o != null && o.Status != OrderStatus.Completed && o.Status != OrderStatus.Cancelled);

                        LowStockItems = new ObservableCollection<InventoryItem>(
                            lowStockItems.Where(item => item != null).Take(5)
                        );

                        RecentRequests = new ObservableCollection<MaterialRequest>(
                            requests.Where(r => r != null)
                                   .OrderByDescending(r => r.RequestDate)
                                   .Take(5)
                        );
                    });
                }

                // --- SUPPLIER METRICS ---
                if (ShowSupplierMetrics && _authService.CurrentUser?.Role == UserRole.Supplier)
                {
                    var supplierProfile = await _userService.GetSupplierProfileAsync(_authService.CurrentUser.AccId);

                    if (supplierProfile != null)
                    {
                        var supplierOrders = await SafeExecuteAsync(() =>
                            _orderService.GetOrdersBySupplierAsync(supplierProfile.SupplierId)
                        );

                        var allInvoices = await SafeExecuteAsync(_invoiceService.GetAllInvoicesAsync);
                        var supplierInvoices = allInvoices
                            .Where(i => i?.Order?.SupplierId == supplierProfile.SupplierId)
                            .ToList();

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            TotalOrders = supplierOrders.Count;
                            PendingOrders = supplierOrders.Count(o => o?.Status == OrderStatus.Pending);
                            ConfirmedOrders = supplierOrders.Count(o => o?.Status == OrderStatus.Confirmed);
                            TotalOrderValue = supplierOrders
                                .Where(o => o != null)
                                .Sum(o => o.TotalAmount);

                            RecentInvoices = new ObservableCollection<Invoice>(
                                supplierInvoices.Where(i => i != null)
                                               .OrderByDescending(i => i.InvoiceDate)
                                               .Take(5)
                            );
                        });
                    }
                }

                Debug.WriteLine("✅ Dashboard UI updated successfully with role-specific data");
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"💥 Critical error in LoadDashboardDataAsync: {ex.Message}");
                MessageBox.Show(
                    $"Error loading dashboard data: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }


        // === NAVIGATION METHODS ===
        private void NavigateToRequests()
        {
            // This should integrate with your navigation system
            // For now, just show a message
            MessageBox.Show("Navigating to Requests Management...", "Navigation",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NavigateToInventory()
        {
            MessageBox.Show("Navigating to Inventory Management...", "Navigation",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NavigateToOrders()
        {
            MessageBox.Show("Navigating to Order Management...", "Navigation",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NavigateToPayments()
        {
            MessageBox.Show("Navigating to Payment Processing...", "Navigation",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NavigateToUsers()
        {
            MessageBox.Show("Navigating to User Management...", "Navigation",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // === HELPER METHODS (keep these) ===
        private decimal SafeGetDecimal(decimal? value)
        {
            return value ?? 0m;
        }

        private int SafeGetInt(int? value)
        {
            return value ?? 0;
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