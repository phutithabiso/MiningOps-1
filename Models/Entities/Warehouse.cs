using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MiningOps.Models.Entities
{
    public class Warehouse
    {
        [Key]
        public int WarehouseId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; }

        [MaxLength(300)]
        public string Location { get; set; }

        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<InventoryItem> InventoryItems { get; set; }
    }
}