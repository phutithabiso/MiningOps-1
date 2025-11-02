using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MiningOps.Models.Entities
{
    public class MaterialRequest
    {
        [Key]
        public int MaterialRequestId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ItemName { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending";

        [Required]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        // Navigation - matches web app
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; }
    }
}