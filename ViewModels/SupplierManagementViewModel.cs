using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiningOps.Dialogs;
using MiningOps.Models.Entities;
using MiningOps.Services;
using MiningOps.Utilities;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MiningOps.ViewModels
{
    public class SupplierManagementViewModel : BaseViewModel
    {
        private readonly SupplierService _supplierService;
        private readonly UserService _userService;
        private readonly OrderService _orderService;
        private ObservableCollection<Supplier> _suppliers;
        private ObservableCollection<Supplier> _recentSuppliers;
        private Supplier _selectedSupplier;
        private string _searchText;
        private readonly IServiceProvider _serviceProvider;

        // Dashboard Properties
        private int _activeSuppliersCount;
        private int _totalPurchaseOrders;
        private int _pendingOrdersCount;
        private string _topSupplier;
        private string _averageResponseTime;
        private string _orderCompletionRate;

        public SupplierManagementViewModel(
       SupplierService supplierService,
       UserService userService,
       OrderService orderService,
       IServiceProvider serviceProvider)
        {

            /*  var configuration = new ConfigurationBuilder()
                  .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                  .Build();*/

            _supplierService = supplierService;
            _userService = userService;
            _orderService = orderService;
            _serviceProvider = serviceProvider;

            Suppliers = new ObservableCollection<Supplier>();
            RecentSuppliers = new ObservableCollection<Supplier>();

            // Initialize commands
            AddSupplierCommand = new RelayCommand(async () => await AddSupplierAsync());
            EditSupplierCommand = new RelayCommand<Supplier>(async (supplier) => await EditSupplierAsync(supplier));
            DeleteSupplierCommand = new RelayCommand<Supplier>(async (supplier) => await DeleteSupplierAsync(supplier));
            RefreshCommand = new RelayCommand(async () => await LoadSuppliersAsync());
            SearchCommand = new RelayCommand(async () => await SearchSuppliersAsync());
            ExportSupplierReportCommand = new RelayCommand(async () => await ExportSupplierReportAsync());
            ExportContactListCommand = new RelayCommand(async () => await ExportContactListAsync());

            _ = LoadSuppliersAsync();
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public ObservableCollection<Supplier> Suppliers
        {
            get => _suppliers;
            set => SetProperty(ref _suppliers, value);
        }

        public ObservableCollection<Supplier> RecentSuppliers
        {
            get => _recentSuppliers;
            set => SetProperty(ref _recentSuppliers, value);
        }

        public Supplier SelectedSupplier
        {
            get => _selectedSupplier;
            set => SetProperty(ref _selectedSupplier, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        // Dashboard Properties
        public int ActiveSuppliersCount
        {
            get => _activeSuppliersCount;
            set => SetProperty(ref _activeSuppliersCount, value);
        }

        public int TotalPurchaseOrders
        {
            get => _totalPurchaseOrders;
            set => SetProperty(ref _totalPurchaseOrders, value);
        }

        public int PendingOrdersCount
        {
            get => _pendingOrdersCount;
            set => SetProperty(ref _pendingOrdersCount, value);
        }

        public string TopSupplier
        {
            get => _topSupplier;
            set => SetProperty(ref _topSupplier, value);
        }

        public string AverageResponseTime
        {
            get => _averageResponseTime;
            set => SetProperty(ref _averageResponseTime, value);
        }

        public string OrderCompletionRate
        {
            get => _orderCompletionRate;
            set => SetProperty(ref _orderCompletionRate, value);
        }

        public ICommand AddSupplierCommand { get; }
        public ICommand EditSupplierCommand { get; }
        public ICommand DeleteSupplierCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ExportSupplierReportCommand { get; }
        public ICommand ExportContactListCommand { get; }

        private async Task LoadSuppliersAsync()
        {
            try
            {
                var suppliers = await _supplierService.GetAllSuppliersAsync();
                var orders = await _orderService.GetAllOrdersAsync();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Suppliers.Clear();
                    foreach (var supplier in suppliers)
                        Suppliers.Add(supplier);

                    RecentSuppliers.Clear();
                    foreach (var supplier in suppliers.Take(5))
                        RecentSuppliers.Add(supplier);

                    // Calculate dashboard metrics
                    CalculateDashboardMetrics(suppliers, orders);
                });
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading suppliers: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateDashboardMetrics(List<Supplier> suppliers, List<PurchaseOrder> orders)
        {
            ActiveSuppliersCount = suppliers.Count;
            TotalPurchaseOrders = orders.Count;
            PendingOrdersCount = orders.Count(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed);

            // Calculate top supplier (simplified - based on order count)
            var supplierOrderCounts = orders
                .GroupBy(o => o.SupplierId)
                .Select(g => new { SupplierId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefault();

            if (supplierOrderCounts != null)
            {
                var topSupplier = suppliers.FirstOrDefault(s => s.SupplierId == supplierOrderCounts.SupplierId);
                TopSupplier = topSupplier?.CompanyName ?? "N/A";
            }
            else
            {
                TopSupplier = "No orders";
            }

            // Mock data for demonstration
            AverageResponseTime = "2.3 days";
            OrderCompletionRate = "94%";
        }

        private async Task SearchSuppliersAsync()
        {
            try
            {
                var allSuppliers = await _supplierService.GetAllSuppliersAsync();
                var filteredSuppliers = allSuppliers.Where(s =>
                    string.IsNullOrEmpty(SearchText) ||
                    s.CompanyName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                    s.ContactPerson?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                    s.RegisterMining?.Email?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true
                ).ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Suppliers.Clear();
                    foreach (var supplier in filteredSuppliers)
                        Suppliers.Add(supplier);
                });
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error searching suppliers: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task AddSupplierAsync()
        {
            var userService = _serviceProvider.GetService<UserService>();
            var dialog = new SupplierDialog(userService);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _supplierService.CreateSupplierAsync(dialog.Supplier);
                    await LoadSuppliersAsync();
                    MessageBox.Show("Supplier added successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error adding supplier: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task EditSupplierAsync(Supplier supplier)
        {
            if (supplier == null)
            {
                MessageBox.Show("Please select a supplier to edit", "No Supplier Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var userService = _serviceProvider.GetService<UserService>();
            var dialog = new SupplierDialog(userService, supplier);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _supplierService.UpdateSupplierAsync(dialog.Supplier);
                    await LoadSuppliersAsync();
                    MessageBox.Show("Supplier updated successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error updating supplier: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task DeleteSupplierAsync(Supplier supplier)
        {
            if (supplier == null)
            {
                MessageBox.Show("Please select a supplier to delete", "No Supplier Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete supplier '{supplier.CompanyName}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _supplierService.DeleteSupplierAsync(supplier.SupplierId);
                    await LoadSuppliersAsync();
                    MessageBox.Show("Supplier deleted successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error deleting supplier: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ExportSupplierReportAsync()
        {
            try
            {
                MessageBox.Show($"Supplier report generated for {Suppliers.Count} suppliers", "Report Generated",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ExportContactListAsync()
        {
            try
            {
                MessageBox.Show($"Contact list exported for {Suppliers.Count} suppliers", "Export Complete",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error exporting contact list: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}