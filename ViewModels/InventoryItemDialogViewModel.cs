using MiningOps.Models.Entities;
using MiningOps.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace MiningOps.ViewModels.Dialogs
{
    public class InventoryItemDialogViewModel : BaseViewModel
    {
        private InventoryItem _inventoryItem;
        private List<Warehouse> _warehouses;
        private bool _isEditMode;

        public InventoryItemDialogViewModel(List<Warehouse> warehouses)
        {
            _inventoryItem = new InventoryItem();
            _warehouses = warehouses;
            _isEditMode = false;

            InitializeCommands();
        }

        public InventoryItemDialogViewModel(List<Warehouse> warehouses, InventoryItem existingItem)
        {
            _inventoryItem = existingItem;
            _warehouses = warehouses;
            _isEditMode = true;

            InitializeCommands();
        }

        public InventoryItem InventoryItem
        {
            get => _inventoryItem;
            set => SetProperty(ref _inventoryItem, value);
        }

        public List<Warehouse> Warehouses
        {
            get => _warehouses;
            set => SetProperty(ref _warehouses, value);
        }

        public string DialogTitle => _isEditMode ? "Edit Inventory Item" : "Add New Inventory Item";

        public ICommand SaveCommand { get; private set; }

        private void InitializeCommands()
        {
            SaveCommand = new RelayCommand(() => { }, CanSave);
        }

        public bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(InventoryItem.ItemName) &&
                   InventoryItem.Quantity >= 0 &&
                   InventoryItem.ReorderLevel >= 0 &&
                   InventoryItem.UnitCost >= 0 &&
                   InventoryItem.WarehouseId > 0;
        }
    }
}