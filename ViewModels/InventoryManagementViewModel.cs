using Microsoft.Extensions.Configuration;
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
    public class InventoryManagementViewModel : BaseViewModel
    {
        private readonly InventoryService _inventoryService;
        private readonly WarehouseService _warehouseService;
        private ObservableCollection<InventoryItem> _inventoryItems;
        private ObservableCollection<Warehouse> _warehouses;
        private InventoryItem _selectedItem;
        private string _searchText;
        private string _warehouseFilter = "All";
        private string _stockStatusFilter = "All";

        // Dashboard Properties
        private decimal _totalInventoryValue;
        private int _outOfStockCount;
        private int _lowStockCount;
        private int _inStockCount;
        private ObservableCollection<InventoryItem> _lowStockItems;

        public InventoryManagementViewModel(
       InventoryService inventoryService,
       WarehouseService warehouseService)
        {
            /* var configuration = new ConfigurationBuilder()
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                 .Build();*/

            _inventoryService = inventoryService;
            _warehouseService = warehouseService;

            InventoryItems = new ObservableCollection<InventoryItem>();
            Warehouses = new ObservableCollection<Warehouse>();
            LowStockItems = new ObservableCollection<InventoryItem>();

            // Initialize commands
            AddItemCommand = new RelayCommand(async () => await AddItemAsync());
            EditItemCommand = new RelayCommand<InventoryItem>(async (item) => await EditItemAsync(item));
            ShowDetailsCommand = new RelayCommand<InventoryItem>(async (item) => await ShowDetailsAsync(item));
            DeleteItemCommand = new RelayCommand<InventoryItem>(async (item) => await DeleteItemAsync(item));
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            SearchCommand = new RelayCommand(async () => await SearchItemsAsync());
            ExportLowStockCommand = new RelayCommand(async () => await ExportLowStockReportAsync());

            _ = LoadDataAsync();
        }

        // Existing Properties
        public ObservableCollection<InventoryItem> InventoryItems
        {
            get => _inventoryItems;
            set => SetProperty(ref _inventoryItems, value);
        }

        public ObservableCollection<Warehouse> Warehouses
        {
            get => _warehouses;
            set => SetProperty(ref _warehouses, value);
        }

        public InventoryItem SelectedItem
        {
            get => _selectedItem;
            set => SetProperty(ref _selectedItem, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public string WarehouseFilter
        {
            get => _warehouseFilter;
            set => SetProperty(ref _warehouseFilter, value);
        }

        public string StockStatusFilter
        {
            get => _stockStatusFilter;
            set => SetProperty(ref _stockStatusFilter, value);
        }

        // Dashboard Properties
        public decimal TotalInventoryValue
        {
            get => _totalInventoryValue;
            set => SetProperty(ref _totalInventoryValue, value);
        }

        public int OutOfStockCount
        {
            get => _outOfStockCount;
            set => SetProperty(ref _outOfStockCount, value);
        }

        public int LowStockCount
        {
            get => _lowStockCount;
            set => SetProperty(ref _lowStockCount, value);
        }

        public int InStockCount
        {
            get => _inStockCount;
            set => SetProperty(ref _inStockCount, value);
        }

        public ObservableCollection<InventoryItem> LowStockItems
        {
            get => _lowStockItems;
            set => SetProperty(ref _lowStockItems, value);
        }

        // Commands
        public ICommand AddItemCommand { get; }
        public ICommand EditItemCommand { get; }
        public ICommand ShowDetailsCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ExportLowStockCommand { get; }

        private async Task LoadDataAsync()
        {
            try
            {
                var items = await SafeExecuteAsync(_inventoryService.GetAllItemsAsync);
                var warehouses = await SafeExecuteAsync(_warehouseService.GetAllWarehousesAsync);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    InventoryItems.Clear();
                    foreach (var item in items.Where(i => i != null))
                        InventoryItems.Add(item);

                    Warehouses.Clear();
                    foreach (var warehouse in warehouses.Where(w => w != null))
                        Warehouses.Add(warehouse);

                    // Calculate dashboard metrics
                    CalculateDashboardMetrics(items);
                });
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error loading inventory: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateDashboardMetrics(List<InventoryItem> items)
        {
            TotalInventoryValue = items.Where(i => i != null)
                .Sum(i => i.Quantity * i.UnitCost);

            OutOfStockCount = items.Count(i => i?.Quantity <= 0);
            LowStockCount = items.Count(i => i?.Quantity > 0 && i.Quantity <= i.ReorderLevel);
            InStockCount = items.Count(i => i?.Quantity > i.ReorderLevel);

            LowStockItems.Clear();
            foreach (var item in items.Where(i => i != null && i.Quantity > 0 && i.Quantity <= i.ReorderLevel).Take(5))
            {
                LowStockItems.Add(item);
            }
        }

        // Add the same SafeExecuteAsync helper to this ViewModel
        private async Task<List<T>> SafeExecuteAsync<T>(Func<Task<List<T>>> serviceCall)
        {
            try
            {
                var result = await serviceCall();
                return result ?? new List<T>();
            }
            catch
            {
                return new List<T>();
            }
        }

        private async Task SearchItemsAsync()
        {
            try
            {
                var allItems = await _inventoryService.GetAllItemsAsync();
                var filteredItems = allItems.Where(item =>
                    (WarehouseFilter == "All" || item.Warehouse?.Name == WarehouseFilter) &&
                    (StockStatusFilter == "All" || GetStockStatus(item) == StockStatusFilter) &&
                    (string.IsNullOrEmpty(SearchText) ||
                     item.ItemName?.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) == true ||
                     item.Description?.Contains(SearchText, System.StringComparison.OrdinalIgnoreCase) == true)
                ).ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    InventoryItems.Clear();
                    foreach (var item in filteredItems)
                        InventoryItems.Add(item);
                });
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error searching inventory: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetStockStatus(InventoryItem item)
        {
            if (item.Quantity <= 0) return "Out of Stock";
            if (item.Quantity <= item.ReorderLevel) return "Low Stock";
            return "In Stock";
        }

        private async Task AddItemAsync()
        {
            var dialog = new InventoryItemDialog(Warehouses.ToList());
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _inventoryService.CreateItemAsync(dialog.InventoryItem);
                    await LoadDataAsync();
                    MessageBox.Show("Item added successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error adding item: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task EditItemAsync(InventoryItem item)
        {
            if (item == null)
            {
                MessageBox.Show("Please select an item to edit", "No Item Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new InventoryItemDialog(Warehouses.ToList(), item);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _inventoryService.UpdateItemAsync(dialog.InventoryItem);
                    await LoadDataAsync();
                    MessageBox.Show("Item updated successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error updating item: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ShowDetailsAsync(InventoryItem item)
        {
            if (item == null)
            {
                MessageBox.Show("Please select an item to view details", "No Item Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create a detailed view dialog
            var detailsDialog = new InventoryDetailsDialog(item);
            detailsDialog.ShowDialog();
        }

        private async Task DeleteItemAsync(InventoryItem item)
        {
            if (item == null)
            {
                MessageBox.Show("Please select an item to delete", "No Item Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete item '{item.ItemName}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _inventoryService.DeleteItemAsync(item.InventoryId);
                    await LoadDataAsync();
                    MessageBox.Show("Item deleted successfully", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error deleting item: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ExportLowStockReportAsync()
        {
            try
            {
                var lowStockItems = await _inventoryService.GetLowStockItemsAsync();
                // Implementation for exporting to CSV/Excel
                MessageBox.Show($"Low stock report generated with {lowStockItems.Count} items", "Report Generated",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}