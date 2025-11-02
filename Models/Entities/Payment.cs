using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiningOps.Models.Entities
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int InvoiceId { get; set; }

        // Make nullable to match database
        public decimal? Amount { get; set; }
        public string PaymentReference { get; set; }

        public DateTime PaidDate { get; set; } = DateTime.UtcNow;
        public int? ApprovedById { get; set; }

        [ForeignKey("InvoiceId")]
        public Invoice Invoice { get; set; }

        [ForeignKey("ApprovedById")]
        public RegisterMining ApprovedBy { get; set; }

        // Safe accessor for UI and calculations
        [NotMapped]
        public decimal SafeAmount => Amount ?? 0m;

        [NotMapped]
        public string SafePaymentReference => PaymentReference ?? $"PAY-{PaymentId:D6}";
    }
}