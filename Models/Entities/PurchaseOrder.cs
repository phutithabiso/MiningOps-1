using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiningOps.Models.Entities
{
    public class PurchaseOrder
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        public int SupplierId { get; set; }

        [ForeignKey(nameof(SupplierId))]
        public virtual Supplier Supplier { get; set; }

        [Required]
        public int RequestedBy { get; set; }

        [ForeignKey(nameof(RequestedBy))]
        public virtual RegisterMining Requester { get; set; }

        public string Currency { get; set; } = "ZAR";

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpectedDeliveryDate { get; set; }

        public decimal TotalAmount { get; set; } = 0m;
        public int? MaterialRequestId { get; set; }
        [ForeignKey(nameof(MaterialRequestId))]
        public virtual MaterialRequest MaterialRequest { get; set; }

        // Navigation
        public virtual ICollection<OrderItem> Items { get; set; }
        public virtual ICollection<Invoice> Invoices { get; set; }
    }

    public enum OrderStatus
    {
        Pending = 0,
        Confirmed = 1,
        Dispatched = 2,
        Completed = 3,
        Cancelled = 4
    }
}