using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiningOps.Models.Entities
{
    public class OrderItem : INotifyPropertyChanged
    {
        private string _itemName;
        private int _quantity;
        private decimal _unitPrice;

        [Key]
        public int OrderItemId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ItemName
        {
            get => _itemName;
            set
            {
                _itemName = value;
                OnPropertyChanged(nameof(ItemName));
            }
        }

        [Required]
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
                OnPropertyChanged(nameof(TotalPrice)); // Update Total when Quantity changes
            }
        }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                _unitPrice = value;
                OnPropertyChanged(nameof(UnitPrice));
                OnPropertyChanged(nameof(TotalPrice)); // Update Total when UnitPrice changes
            }
        }

        [NotMapped]
        public decimal TotalPrice => Quantity * UnitPrice;

        // Foreign Key to PurchaseOrder
        public int PurchaseOrderId { get; set; }

        [ForeignKey("PurchaseOrderId")]
        public virtual PurchaseOrder PurchaseOrder { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}