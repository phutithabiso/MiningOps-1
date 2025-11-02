using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiningOps.Models.Entities
{
    public class Supervisor
    {
        [Key]
        public int SupervisorId { get; set; }

        [Required]
        public int AccId { get; set; }

        [ForeignKey(nameof(AccId))]
        public virtual RegisterMining RegisterMining { get; set; }

        public string Team { get; set; }

        [MaxLength(200)]
        public string MineLocation { get; set; }

        [MaxLength(20)]
        public string Shift { get; set; }

        public bool CanViewReports { get; set; } = true;
        public bool CanManageTasks { get; set; } = true;
    }
}