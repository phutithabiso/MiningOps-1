using MiningOps.Models.Entities;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MiningOps.Dialogs
{
    public partial class PurchaseOrderDialog : Window, INotifyPropertyChanged
    {
        private ObservableCollection<OrderItem> _orderItems;
        private decimal _totalAmount;

        public PurchaseOrder PurchaseOrder { get; private set; }
        public ObservableCollection<Supplier> Suppliers { get; set; }
        public ObservableCollection<MaterialRequest> MaterialRequests { get; set; }

        public ObservableCollection<OrderItem> OrderItems
        {
            get => _orderItems;
            set
            {
                _orderItems = value;
                OnPropertyChanged(nameof(OrderItems));
                CalculateTotalAmount();
            }
        }

        public Supplier SelectedSupplier { get; set; }
        public MaterialRequest SelectedMaterialRequest { get; set; }
        public string Currency { get; set; } = "ZAR";
        public System.DateTime? ExpectedDeliveryDate { get; set; }

        public decimal TotalAmount
        {
            get => _totalAmount;
            set
            {
                _totalAmount = value;
                OnPropertyChanged(nameof(TotalAmount));
            }
        }

        public PurchaseOrderDialog(ObservableCollection<Supplier> suppliers,
                                 ObservableCollection<MaterialRequest> materialRequests,
                                 PurchaseOrder existingOrder = null)
        {
            InitializeComponent();

            Suppliers = suppliers ?? new ObservableCollection<Supplier>();
            MaterialRequests = materialRequests ?? new ObservableCollection<MaterialRequest>();
            OrderItems = new ObservableCollection<OrderItem>();

            if (existingOrder != null)
            {
                PurchaseOrder = existingOrder;
                SelectedSupplier = Suppliers.FirstOrDefault(s => s.SupplierId == existingOrder.SupplierId);
                SelectedMaterialRequest = MaterialRequests.FirstOrDefault(mr => mr.MaterialRequestId == existingOrder.MaterialRequestId);
                Currency = existingOrder.Currency ?? "ZAR";
                ExpectedDeliveryDate = existingOrder.ExpectedDeliveryDate;

                if (existingOrder.Items != null)
                {
                    foreach (var item in existingOrder.Items)
                    {
                        OrderItems.Add(new OrderItem
                        {
                            ItemName = item.ItemName,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        });
                    }
                }
                CalculateTotalAmount();
            }
            else
            {
                PurchaseOrder = new PurchaseOrder
                {
                    CreatedAt = System.DateTime.UtcNow,
                    Status = OrderStatus.Pending
                };
            }

            DataContext = this;
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            var newItem = new OrderItem
            {
                ItemName = "New Item",
                Quantity = 1,
                UnitPrice = 0m
            };
            OrderItems.Add(newItem);
            CalculateTotalAmount();
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is OrderItem item)
            {
                OrderItems.Remove(item);
                CalculateTotalAmount();
            }
        }

        // REMOVED: All DataGrid event handlers that were causing refresh issues

        private void CalculateTotalAmount()
        {
            // Simple calculation without any UI refresh
            TotalAmount = OrderItems.Sum(item => item.Quantity * item.UnitPrice);
        }
        private void MaterialRequest_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectedMaterialRequest == null) return;

            // Check if we already have this material request in our items
            var existingItem = OrderItems.FirstOrDefault(item =>
                item.ItemName.Equals(SelectedMaterialRequest.ItemName, System.StringComparison.OrdinalIgnoreCase));

            if (existingItem != null)
            {
                // Update quantity of existing item
                existingItem.Quantity = SelectedMaterialRequest.Quantity;
                MessageBox.Show($"Updated quantity for {SelectedMaterialRequest.ItemName} to {SelectedMaterialRequest.Quantity}",
                    "Quantity Updated", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Add new item from material request
                var newItem = new OrderItem
                {
                    ItemName = SelectedMaterialRequest.ItemName,
                    Quantity = SelectedMaterialRequest.Quantity,
                    UnitPrice = 0m // User needs to enter price
                };

                OrderItems.Add(newItem);
                MessageBox.Show($"Added item from material request: {SelectedMaterialRequest.ItemName} (Quantity: {SelectedMaterialRequest.Quantity})",
                    "Item Added", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            CalculateTotalAmount();
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSupplier == null)
            {
                MessageBox.Show("Please select a supplier", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (OrderItems.Count == 0)
            {
                MessageBox.Show("Please add at least one order item", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate item data
            var invalidItems = OrderItems.Where(item =>
                string.IsNullOrWhiteSpace(item.ItemName) ||
                item.Quantity <= 0 ||
                item.UnitPrice <= 0).ToList();

            if (invalidItems.Any())
            {
                MessageBox.Show("Please ensure all items have valid names, quantities > 0, and unit prices > 0",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // FIX: Recalculate total amount before saving to ensure it's current
            CalculateTotalAmount();

            // Update purchase order with current values
            PurchaseOrder.SupplierId = SelectedSupplier.SupplierId;
            PurchaseOrder.MaterialRequestId = SelectedMaterialRequest?.MaterialRequestId;
            PurchaseOrder.Currency = Currency;
            PurchaseOrder.ExpectedDeliveryDate = ExpectedDeliveryDate;
            PurchaseOrder.TotalAmount = TotalAmount; // This should now have the correct value

            // Convert ObservableCollection to List for entity
            PurchaseOrder.Items = OrderItems.Select(oi => new OrderItem
            {
                ItemName = oi.ItemName,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice
                // TotalPrice is calculated automatically via [NotMapped] property
            }).ToList();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (DialogResult != true)
            {
                DialogResult = false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}