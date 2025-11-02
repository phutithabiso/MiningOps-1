using MiningOps.Models.Entities;
using MiningOps.Utilities;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace MiningOps.ViewModels.Dialogs
{
    public class PurchaseOrderDialogViewModel : BaseViewModel
    {
        private PurchaseOrder _purchaseOrder;
        private OrderItem _currentItem;
        private ObservableCollection<Supplier> _suppliers;
        private ObservableCollection<MaterialRequest> _materialRequests;
        private int _selectedMaterialRequestId;

        public PurchaseOrderDialogViewModel(ObservableCollection<Supplier> suppliers,
                                          ObservableCollection<MaterialRequest> materialRequests,
                                          PurchaseOrder existingOrder = null)
        {
            _suppliers = suppliers;
            _materialRequests = materialRequests;

            _purchaseOrder = existingOrder ?? new PurchaseOrder
            {
                CreatedAt = System.DateTime.UtcNow,
                Status = OrderStatus.Pending,
                Items = new System.Collections.Generic.List<OrderItem>(),
                Currency = "ZAR"
            };

            _currentItem = new OrderItem();
            InitializeCommands();
        }

        public PurchaseOrder PurchaseOrder
        {
            get => _purchaseOrder;
            set => SetProperty(ref _purchaseOrder, value);
        }

        public OrderItem CurrentItem
        {
            get => _currentItem;
            set => SetProperty(ref _currentItem, value);
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

        public int SelectedMaterialRequestId
        {
            get => _selectedMaterialRequestId;
            set => SetProperty(ref _selectedMaterialRequestId, value);
        }

        // FIXED: Calculate total from Quantity * UnitPrice directly
        public decimal TotalAmount => PurchaseOrder.Items?.Sum(item => item.Quantity * item.UnitPrice) ?? 0m;

        public ICommand AddItemCommand { get; private set; }
        public ICommand RemoveItemCommand { get; private set; }
        public ICommand SaveCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }

        private void InitializeCommands()
        {
            AddItemCommand = new RelayCommand(AddItem, CanAddItem);
            RemoveItemCommand = new RelayCommand<OrderItem>(RemoveItem);
            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
        }

        private void AddItem()
        {
            if (CurrentItem != null && !string.IsNullOrWhiteSpace(CurrentItem.ItemName) && CurrentItem.Quantity > 0)
            {
                // Create a new instance to avoid reference issues
                var newItem = new OrderItem
                {
                    ItemName = CurrentItem.ItemName,
                    // REMOVED: Description doesn't exist in database
                    Quantity = CurrentItem.Quantity,
                    UnitPrice = CurrentItem.UnitPrice
                    // TotalPrice is calculated automatically via [NotMapped] property
                };

                // Convert List to ObservableCollection for UI binding
                var itemsList = PurchaseOrder.Items?.ToList() ?? new System.Collections.Generic.List<OrderItem>();
                itemsList.Add(newItem);
                PurchaseOrder.Items = itemsList;

                // Reset current item for next entry
                CurrentItem = new OrderItem();

                // Update total amount
                OnPropertyChanged(nameof(TotalAmount));
                OnPropertyChanged(nameof(PurchaseOrder));
            }
        }

        private void RemoveItem(OrderItem item)
        {
            if (item != null)
            {
                var itemsList = PurchaseOrder.Items?.ToList() ?? new System.Collections.Generic.List<OrderItem>();
                itemsList.Remove(item);
                PurchaseOrder.Items = itemsList;

                OnPropertyChanged(nameof(TotalAmount));
                OnPropertyChanged(nameof(PurchaseOrder));
            }
        }

        private bool CanAddItem()
        {
            return CurrentItem != null &&
                   !string.IsNullOrWhiteSpace(CurrentItem.ItemName) &&
                   CurrentItem.Quantity > 0 &&
                   CurrentItem.UnitPrice >= 0;
        }

        private bool CanSave()
        {
            return (PurchaseOrder.Items?.Count ?? 0) > 0 &&
                   PurchaseOrder.SupplierId > 0;
        }

        private void Save()
        {
            // Calculate total amount before saving
            PurchaseOrder.TotalAmount = TotalAmount;

            // Close dialog with success
            CloseDialog(true);
        }

        private void Cancel()
        {
            CloseDialog(false);
        }

        private void CloseDialog(bool result)
        {
            var window = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>()
                .FirstOrDefault(w => w.DataContext == this);

            if (window != null)
            {
                window.DialogResult = result;
                window.Close();
            }
        }

        // Helper method to get items as ObservableCollection for UI binding
        public ObservableCollection<OrderItem> GetItemsForUI()
        {
            return new ObservableCollection<OrderItem>(PurchaseOrder.Items ?? new System.Collections.Generic.List<OrderItem>());
        }
    }
}