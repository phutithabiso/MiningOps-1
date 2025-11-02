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
using System.Windows.Controls;
using System.Windows.Input;

namespace MiningOps.ViewModels
{
    public class OrderManagementViewModel : BaseViewModel
    {
        private readonly OrderService _orderService;
        private readonly SupplierService _supplierService;
        private readonly RequestService _requestService;
        private ObservableCollection<PurchaseOrder> _orders;
        private ObservableCollection<Supplier> _suppliers;
        private ObservableCollection<MaterialRequest> _materialRequests;
        private PurchaseOrder _selectedOrder;
        private string _searchText;
        private string _statusFilter = "All";
        private readonly IAuthenticationService _authService;

        // Dashboard Properties
        private int _pendingOrdersCount;
        private int _confirmedOrdersCount;
        private int _dispatchedOrdersCount;
        private int _completedOrdersCount;
        private int _cancelledOrdersCount;
        private decimal _totalOrdersValue;
        private ObservableCollection<SupplierStats> _topSuppliers;
        private ObservableCollection<PurchaseOrder> _recentOrders;
        private bool _isLoading;
        // Add to OrderManagementViewModel
        public bool IsAdmin => _authService.IsInRole(UserRole.Admin);
        public OrderManagementViewModel(OrderService orderService, SupplierService supplierService, RequestService requestService, IAuthenticationService authService)
        {
            /*var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();*/

            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _supplierService = supplierService ?? throw new ArgumentNullException(nameof(orderService));
            _requestService = requestService ?? throw new ArgumentNullException(nameof(orderService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService)); // Initialize      

            Orders = new ObservableCollection<PurchaseOrder>();
            Suppliers = new ObservableCollection<Supplier>();
            MaterialRequests = new ObservableCollection<MaterialRequest>();
            TopSuppliers = new ObservableCollection<SupplierStats>();
            RecentOrders = new ObservableCollection<PurchaseOrder>();

            // Initialize commands
            CreateOrderCommand = new RelayCommand(async () => await CreateOrderAsync());
            EditOrderCommand = new RelayCommand<PurchaseOrder>(async (order) => await EditOrderAsync(order));
            ViewOrderDetailsCommand = new RelayCommand<PurchaseOrder>(async (order) => await ViewOrderDetailsAsync(order));
            UpdateStatusCommand = new RelayCommand<PurchaseOrder>(async (order) => await UpdateOrderStatusAsync(order));
            DeleteOrderCommand = new RelayCommand<PurchaseOrder>(async (order) => await DeleteOrderAsync(order));
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            SearchCommand = new RelayCommand(async () => await SearchOrdersAsync());

            _ = LoadDataAsync();
        }

        // Main Collections
        public ObservableCollection<PurchaseOrder> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }

        public ObservableCollection<Supplier> Suppliers
        {
            get => _suppliers;
            set => SetProperty(ref _suppliers, value);
        }

        public ObservableCollection<MaterialRequest> MaterialRequests
        {
            get => _materialRequests;
            set => SetProperty(ref _materialRequests, value);
        }

        public PurchaseOrder SelectedOrder
        {
            get => _selectedOrder;
            set => SetProperty(ref _selectedOrder, value);
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

        // Dashboard Properties
        public int PendingOrdersCount
        {
            get => _pendingOrdersCount;
            set => SetProperty(ref _pendingOrdersCount, value);
        }

        public int ConfirmedOrdersCount
        {
            get => _confirmedOrdersCount;
            set => SetProperty(ref _confirmedOrdersCount, value);
        }

        public int DispatchedOrdersCount
        {
            get => _dispatchedOrdersCount;
            set => SetProperty(ref _dispatchedOrdersCount, value);
        }

        public int CompletedOrdersCount
        {
            get => _completedOrdersCount;
            set => SetProperty(ref _completedOrdersCount, value);
        }

        public int CancelledOrdersCount
        {
            get => _cancelledOrdersCount;
            set => SetProperty(ref _cancelledOrdersCount, value);
        }

        public decimal TotalOrdersValue
        {
            get => _totalOrdersValue;
            set => SetProperty(ref _totalOrdersValue, value);
        }

        public ObservableCollection<SupplierStats> TopSuppliers
        {
            get => _topSuppliers;
            set => SetProperty(ref _topSuppliers, value);
        }

        public ObservableCollection<PurchaseOrder> RecentOrders
        {
            get => _recentOrders;
            set => SetProperty(ref _recentOrders, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        // Computed Properties for Dashboard
        public int TotalOrders => Orders.Count;

        public string AverageOrderValue
        {
            get
            {
                return TotalOrders > 0 ? (TotalOrdersValue / TotalOrders).ToString("C2") : "$0.00";
            }
        }

        public string CompletionRate
        {
            get
            {
                return TotalOrders > 0 ? ((double)CompletedOrdersCount / TotalOrders * 100).ToString("0.0") + "%" : "0%";
            }
        }

        // Commands
        public ICommand CreateOrderCommand { get; }
        public ICommand EditOrderCommand { get; }
        public ICommand ViewOrderDetailsCommand { get; }
        public ICommand UpdateStatusCommand { get; }
        public ICommand DeleteOrderCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }

        private async Task LoadDataAsync()
        {
            IsLoading = true;

            try
            {
                Debug.WriteLine("🔄 OrderManagementViewModel: Starting data load...");

                // Get the current user
                var currentUser = _authService.CurrentUser;
                if (currentUser == null)
                {
                    Debug.WriteLine("❌ No current user found");
                    return;
                }

                // Load data in parallel for better performance
                Task<List<PurchaseOrder>> ordersTask;
                if (currentUser.Role == UserRole.Admin)
                {
                    ordersTask = SafeExecuteAsync(_orderService.GetAllOrdersAsync);
                }
                else
                {
                    // For non-admin, load only the current user's orders
                    ordersTask = SafeExecuteAsync(() => _orderService.GetOrdersByRequesterAsync(currentUser.AccId));
                }

                var suppliersTask = SafeExecuteAsync(_supplierService.GetAllSuppliersAsync);
                var requestsTask = SafeExecuteAsync(_requestService.GetAllRequestsAsync);

                await Task.WhenAll(ordersTask, suppliersTask, requestsTask);

                var orders = await ordersTask;
                var suppliers = await suppliersTask;
                var requests = await requestsTask;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // Clear existing collections
                        Orders.Clear();
                        Suppliers.Clear();
                        MaterialRequests.Clear();
                        TopSuppliers.Clear();
                        RecentOrders.Clear();

                        // Populate main collections
                        foreach (var order in orders.Where(o => o != null))
                            Orders.Add(order);

                        foreach (var supplier in suppliers.Where(s => s != null))
                            Suppliers.Add(supplier);

                        foreach (var request in requests.Where(r => r != null && r.Status == "Approved"))
                            MaterialRequests.Add(request);

                        // Calculate and update dashboard metrics
                        CalculateOrderMetrics(orders);

                        Debug.WriteLine($"✅ OrderManagementViewModel: Loaded {orders.Count} orders, {suppliers.Count} suppliers, {requests.Count} requests");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"❌ Error in UI update: {ex.Message}");
                        MessageBox.Show($"Error updating UI: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"💥 OrderManagementViewModel.LoadDataAsync failed: {ex.Message}");
                MessageBox.Show($"Error loading order data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void CalculateOrderMetrics(List<PurchaseOrder> filteredOrders)
        {
            try
            {
                // Basic status counts (from filtered orders)
                PendingOrdersCount = filteredOrders.Count(o => o.Status == OrderStatus.Pending);
                ConfirmedOrdersCount = filteredOrders.Count(o => o.Status == OrderStatus.Confirmed);
                DispatchedOrdersCount = filteredOrders.Count(o => o.Status == OrderStatus.Dispatched);
                CompletedOrdersCount = filteredOrders.Count(o => o.Status == OrderStatus.Completed);
                CancelledOrdersCount = filteredOrders.Count(o => o.Status == OrderStatus.Cancelled);

                // Financial calculations (from filtered orders)
                TotalOrdersValue = filteredOrders.Where(o => o != null).Sum(o => o.TotalAmount);

                // For Top Suppliers - get ALL orders to calculate supplier statistics
                List<PurchaseOrder> allOrdersForSuppliers;
                if (_authService.IsInRole(UserRole.Admin))
                {
                    allOrdersForSuppliers = await _orderService.GetAllOrdersAsync();
                }
                else
                {
                    // Even for non-admin, we want to show all suppliers but with counts from their own orders only
                    allOrdersForSuppliers = filteredOrders; // Use the filtered orders for non-admin
                }

                // Top suppliers - Show ALL suppliers with their order counts
                var supplierStats = Suppliers
                    .Where(s => s != null)
                    .Select(supplier => new SupplierStats
                    {
                        CompanyName = supplier.CompanyName,
                        OrderCount = allOrdersForSuppliers.Count(o => o.Supplier?.SupplierId == supplier.SupplierId),
                        TotalOrderValue = allOrdersForSuppliers.Where(o => o.Supplier?.SupplierId == supplier.SupplierId).Sum(o => o.TotalAmount)
                    })
                    .OrderByDescending(x => x.OrderCount)
                    .ThenByDescending(x => x.TotalOrderValue)
                    .Take(5)
                    .ToList();

                TopSuppliers.Clear();
                foreach (var stat in supplierStats)
                {
                    TopSuppliers.Add(stat);
                }

                // Recent orders (from filtered orders)
                var recentOrders = filteredOrders
                    .Where(o => o != null)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(5)
                    .ToList();

                RecentOrders.Clear();
                foreach (var order in recentOrders)
                    RecentOrders.Add(order);

                // Notify property changes
                OnPropertyChanged(nameof(TotalOrders));
                OnPropertyChanged(nameof(AverageOrderValue));
                OnPropertyChanged(nameof(CompletionRate));

                Debug.WriteLine($"📊 Order Metrics Calculated - Total: {TotalOrders}, Value: {TotalOrdersValue:C}, Completed: {CompletedOrdersCount}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error calculating order metrics: {ex.Message}");
            }
        }

        private async Task SearchOrdersAsync()
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser == null) return;

                List<PurchaseOrder> allOrders;
                if (currentUser.Role == UserRole.Admin)
                {
                    allOrders = await _orderService.GetAllOrdersAsync();
                }
                else
                {
                    allOrders = await _orderService.GetOrdersByRequesterAsync(currentUser.AccId);
                }

                var filteredOrders = allOrders.Where(order =>
                    (StatusFilter == "All" || order.Status.ToString() == StatusFilter) &&
                    (string.IsNullOrEmpty(SearchText) ||
                     order.OrderId.ToString().Contains(SearchText) ||
                     order.Supplier?.CompanyName?.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) == true ||
                     (order.Items != null && order.Items.Any(item => item.ItemName.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase))))
                ).ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Orders.Clear();
                    foreach (var order in filteredOrders)
                        Orders.Add(order);

                    // Recalculate metrics for filtered results
                    CalculateOrderMetrics(filteredOrders);
                });

                Debug.WriteLine($"🔍 Order Search: {filteredOrders.Count} orders match filter '{StatusFilter}' and search '{SearchText}'");
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"❌ Error searching orders: {ex.Message}");
                MessageBox.Show($"Error searching orders: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Add authorization helper method
        private bool CanModifyOrder(PurchaseOrder order)
        {
            var currentUser = _authService.CurrentUser;
            if (currentUser == null) return false;

            // Admin can modify any order, non-admin can only modify their own orders
            return currentUser.Role == UserRole.Admin || order.RequestedBy == currentUser.AccId;
        }

        private async Task EditOrderAsync(PurchaseOrder order)
        {
            if (order == null)
            {
                MessageBox.Show("Please select an order to edit", "No Order Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Add authorization check
            if (!CanModifyOrder(order))
            {
                MessageBox.Show("You are not authorized to edit this order.", "Access Denied",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dialog = new PurchaseOrderDialog(
                    new ObservableCollection<Supplier>(Suppliers),
                    new ObservableCollection<MaterialRequest>(MaterialRequests),
                    order
                );

                if (dialog.ShowDialog() == true)
                {
                    await _orderService.UpdateOrderAsync(dialog.PurchaseOrder);
                    await LoadDataAsync();
                    MessageBox.Show("Order updated successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    Debug.WriteLine($"✅ Updated order ID: {order.OrderId}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"❌ Error updating order: {ex.Message}");
                MessageBox.Show($"Error updating order: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CreateOrderAsync()
        {
            try
            {
                if (!Suppliers.Any())
                {
                    MessageBox.Show("No suppliers available. Please add suppliers first.", "No Suppliers",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                // Check if user is authenticated
                if (!_authService.IsAuthenticated || _authService.CurrentUser == null)
                {
                    MessageBox.Show("You must be logged in to create an order.", "Authentication Required",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var dialog = new PurchaseOrderDialog(
                    new ObservableCollection<Supplier>(Suppliers),
                    new ObservableCollection<MaterialRequest>(MaterialRequests)
                );

                if (dialog.ShowDialog() == true)
                {
                    // Set requested by (current user) - this should come from authentication context
                    dialog.PurchaseOrder.RequestedBy = _authService.CurrentUser.AccId;
                    var newOrder = await _orderService.CreateOrderAsync(dialog.PurchaseOrder);

                    await LoadDataAsync();
                    MessageBox.Show("Purchase order created successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    Debug.WriteLine($"✅ Created new order ID: {newOrder.OrderId}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"❌ Error creating order: {ex.Message}");
                MessageBox.Show($"Error creating order: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /* private async Task EditOrderAsync(PurchaseOrder order)
          {
              if (order == null)
              {
                  MessageBox.Show("Please select an order to edit", "No Order Selected",
                      MessageBoxButton.OK, MessageBoxImage.Warning);
                  return;
              }

              try
              {
                  var dialog = new PurchaseOrderDialog(
                      new ObservableCollection<Supplier>(Suppliers),
                      new ObservableCollection<MaterialRequest>(MaterialRequests),
                      order
                  );

                  if (dialog.ShowDialog() == true)
                  {
                      await _orderService.UpdateOrderAsync(dialog.PurchaseOrder);
                      await LoadDataAsync();
                      MessageBox.Show("Order updated successfully", "Success",
                          MessageBoxButton.OK, MessageBoxImage.Information);

                      Debug.WriteLine($"✅ Updated order ID: {order.OrderId}");
                  }
              }
              catch (System.Exception ex)
              {
                  Debug.WriteLine($"❌ Error updating order: {ex.Message}");
                  MessageBox.Show($"Error updating order: {ex.Message}", "Error",
                      MessageBoxButton.OK, MessageBoxImage.Error);
              }
          }
          */
        private async Task ViewOrderDetailsAsync(PurchaseOrder order)
        {
            if (order == null)
            {
                MessageBox.Show("Please select an order to view details", "No Order Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Add authorization check
            if (!CanModifyOrder(order))
            {
                MessageBox.Show("You are not authorized to view this order.", "Access Denied",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var detailsDialog = new OrderDetailsDialog(order);
                detailsDialog.ShowDialog();

                Debug.WriteLine($"🔍 Viewed details for order ID: {order.OrderId}");
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"❌ Error viewing order details: {ex.Message}");
                MessageBox.Show($"Error viewing order details: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task UpdateOrderStatusAsync(PurchaseOrder order)
        {
            if (order == null) return;

            // Add authorization check
            if (!CanModifyOrder(order))
            {
                MessageBox.Show("You are not authorized to update this order's status.", "Access Denied",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var statusDialog = new Window
                {
                    Title = "Update Order Status",
                    Width = 300,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var stackPanel = new StackPanel { Margin = new Thickness(20) };
                stackPanel.Children.Add(new TextBlock
                {
                    Text = "Select new status:",
                    Margin = new Thickness(0, 0, 0, 10),
                    FontWeight = FontWeights.SemiBold
                });

                var statusComboBox = new ComboBox
                {
                    ItemsSource = System.Enum.GetValues(typeof(OrderStatus)),
                    SelectedItem = order.Status,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                stackPanel.Children.Add(statusComboBox);

                // Add status description
                var statusDescription = new TextBlock
                {
                    Text = GetStatusDescription((OrderStatus)statusComboBox.SelectedItem),
                    FontSize = 11,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 10)
                };
                stackPanel.Children.Add(statusDescription);

                // Update description when selection changes
                statusComboBox.SelectionChanged += (s, e) =>
                {
                    if (statusComboBox.SelectedItem != null)
                    {
                        statusDescription.Text = GetStatusDescription((OrderStatus)statusComboBox.SelectedItem);
                    }
                };

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                };

                var okButton = new Button
                {
                    Content = "Update",
                    Width = 80,
                    Margin = new Thickness(0, 0, 10, 0),
                    Background = System.Windows.Media.Brushes.Green,
                    Foreground = System.Windows.Media.Brushes.White
                };

                var cancelButton = new Button
                {
                    Content = "Cancel",
                    Width = 80,
                    Background = System.Windows.Media.Brushes.Gray,
                    Foreground = System.Windows.Media.Brushes.White
                };

                okButton.Click += (s, e) => { statusDialog.DialogResult = true; statusDialog.Close(); };
                cancelButton.Click += (s, e) => { statusDialog.DialogResult = false; statusDialog.Close(); };

                buttonPanel.Children.Add(okButton);
                buttonPanel.Children.Add(cancelButton);
                stackPanel.Children.Add(buttonPanel);

                statusDialog.Content = stackPanel;

                if (statusDialog.ShowDialog() == true)
                {
                    var newStatus = (OrderStatus)statusComboBox.SelectedItem;
                    await _orderService.UpdateOrderStatusAsync(order.OrderId, newStatus);

                    // AUTO-CREATE INVOICE when order is confirmed
                    if (newStatus == OrderStatus.Confirmed)
                    {
                        try
                        {
                            var invoice = await _orderService.CreateInvoiceFromOrderAsync(order.OrderId);
                            MessageBox.Show($"Invoice created: {invoice.InvoiceReference}", "Invoice Generated",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error creating invoice: {ex.Message}", "Warning",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }

                    await LoadDataAsync();
                    MessageBox.Show("Order status updated successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    Debug.WriteLine($"✅ Updated order {order.OrderId} status to {newStatus}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"❌ Error updating order status: {ex.Message}");
                MessageBox.Show($"Error updating order status: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private string GetStatusDescription(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Order has been created but not yet confirmed by supplier",
                OrderStatus.Confirmed => "Supplier has confirmed the order and will process it",
                OrderStatus.Dispatched => "Order has been shipped and is in transit",
                OrderStatus.Completed => "Order has been delivered and completed successfully",
                OrderStatus.Cancelled => "Order has been cancelled and will not be processed",
                _ => "Unknown status"
            };
        }

        private async Task DeleteOrderAsync(PurchaseOrder order)
        {
            if (order == null)
            {
                MessageBox.Show("Please select an order to delete", "No Order Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Add authorization check
            if (!CanModifyOrder(order))
            {
                MessageBox.Show("You are not authorized to delete this order.", "Access Denied",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete order #{order.OrderId} from {order.Supplier?.CompanyName}?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _orderService.DeleteOrderAsync(order.OrderId);
                    await LoadDataAsync();
                    MessageBox.Show("Order deleted successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    Debug.WriteLine($"🗑️ Deleted order ID: {order.OrderId}");
                }
                catch (System.Exception ex)
                {
                    Debug.WriteLine($"❌ Error deleting order: {ex.Message}");
                    MessageBox.Show($"Error deleting order: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

    // Supplier Stats class for dashboard display
    public class SupplierStats : BaseViewModel
    {
        private string _companyName;
        private int _orderCount;
        private decimal _totalOrderValue;

        public string CompanyName
        {
            get => _companyName;
            set => SetProperty(ref _companyName, value);
        }

        public int OrderCount
        {
            get => _orderCount;
            set => SetProperty(ref _orderCount, value);
        }

        public decimal TotalOrderValue
        {
            get => _totalOrderValue;
            set => SetProperty(ref _totalOrderValue, value);
        }
    }
}