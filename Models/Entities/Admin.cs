using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiningOps.Models.Entities
{
    public class Admin
    {
        [Key]
        public int AdminId { get; set; }

        [Required]
        public int AccId { get; set; }

        [ForeignKey(nameof(AccId))]
        public virtual RegisterMining RegisterMining { get; set; }

        public string Department { get; set; }
        public bool CanManageUsers { get; set; } = true;
        public bool CanApproveRequests { get; set; } = true;
    }
}