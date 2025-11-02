using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiningOps.Models.Entities
{
    public class Supplier
    {
        [Key]
        public int SupplierId { get; set; }

        [Required]
        public int AccId { get; set; }

        [ForeignKey(nameof(AccId))]
        public virtual RegisterMining RegisterMining { get; set; }

        [Required]
        [MaxLength(200)]
        public string CompanyName { get; set; }

        [Required]
        [MaxLength(100)]
        public string ContactPerson { get; set; }

        [MaxLength(300)]
        public string Address { get; set; }

        public bool CanViewOrders { get; set; } = true;
        public bool CanManageInventory { get; set; } = false;
        public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; }

    }
}