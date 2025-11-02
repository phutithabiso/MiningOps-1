using System;
using System.ComponentModel.DataAnnotations;

namespace MiningOps.Models.Entities
{
    public class RegisterMining
    {
        [Key]
        public int AccId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        [Phone]
        [MaxLength(15)]
        public string PhoneNumber { get; set; }

        [Required]
        [MaxLength(500)]
        public string PasswordHash { get; set; }

        [Required]
        [MaxLength(200)]
        public string Salt { get; set; }

        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Admin AdminProfile { get; set; }
        public virtual Supervisor SupervisorProfile { get; set; }
        public virtual Supplier SupplierProfile { get; set; }
    }

    public enum UserRole
    {
        Admin,
        Supervisor,
        Supplier
    }
}