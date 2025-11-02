using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiningOps.Models.Entities
{
    public class InventoryItem
    {
        [Key]
        public int InventoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string ItemName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        // CHANGED: Non-nullable with defaults to match web app
        public int Quantity { get; set; } = 0;
        public decimal UnitCost { get; set; } = 0m;
        public int ReorderLevel { get; set; } = 0;

        [Required]
        public int WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }

        // CHANGED: Match web app field name
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // REMOVED: CreatedAt and UpdatedAt (not in web app)

        // Helper methods for business logic
        public bool IsLowStock => Quantity <= ReorderLevel;
        public decimal TotalValue => Quantity * UnitCost;
    }
}