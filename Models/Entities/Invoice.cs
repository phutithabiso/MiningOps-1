using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiningOps.Models.Entities
{
    public class Invoice
    {
        [Key]
        public int InvoiceId { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey(nameof(OrderId))]
        public virtual PurchaseOrder Order { get; set; }

        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

        // Make problematic fields nullable with safe accessors
        public DateTime? DueDate { get; set; }
        public decimal? Amount { get; set; }
        public InvoiceStatus? Status { get; set; }
        public string InvoiceReference { get; set; }
        public string InvoiceFilePath { get; set; }

        // Safe accessor properties for UI binding
        [NotMapped]
        public DateTime SafeDueDate => DueDate ?? DateTime.UtcNow.AddDays(30);

        [NotMapped]
        public decimal SafeAmount => Amount ?? 0m;

        [NotMapped]
        public InvoiceStatus SafeStatus => Status ?? InvoiceStatus.Unpaid;

        [NotMapped]
        public string SafeInvoiceReference => InvoiceReference ?? $"INV-{InvoiceId:D6}";
    }
    public enum InvoiceStatus
    {
        Draft,
        Sent,
        Unpaid,
        Partial,
        Paid,
        Overdue,
        Cancelled
    }
}