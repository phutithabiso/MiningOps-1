using Microsoft.EntityFrameworkCore;
using MiningOps.Models.Entities;

namespace MiningOps.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Only include DbSets you actually have entities for
        public DbSet<RegisterMining> RegisterMiningDb { get; set; }
        public DbSet<Admin> AdminProfiles { get; set; }
        public DbSet<Supervisor> SupervisorProfiles { get; set; }
        public DbSet<Supplier> SupplierProfiles { get; set; }
        public DbSet<Warehouse> WarehousesDb { get; set; }
        public DbSet<InventoryItem> InventoryDb { get; set; }
        public DbSet<OrderItem> OrderItemsDb { get; set; }
        public DbSet<PurchaseOrder> PurchaseOrdersDb { get; set; }
        public DbSet<MaterialRequest> MaterialRequestsDb { get; set; }
        public DbSet<Invoice> InvoicesDb { get; set; }
        public DbSet<Payment> PaymentsDb { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Explicitly configure primary keys for ALL entities
            modelBuilder.Entity<RegisterMining>().HasKey(r => r.AccId);
            modelBuilder.Entity<Admin>().HasKey(a => a.AdminId);
            modelBuilder.Entity<Supervisor>().HasKey(s => s.SupervisorId);
            modelBuilder.Entity<Supplier>().HasKey(s => s.SupplierId);
            modelBuilder.Entity<Warehouse>().HasKey(w => w.WarehouseId);
            modelBuilder.Entity<InventoryItem>().HasKey(i => i.InventoryId);
            modelBuilder.Entity<OrderItem>().HasKey(oi => oi.OrderItemId);
            modelBuilder.Entity<PurchaseOrder>().HasKey(po => po.OrderId);
            modelBuilder.Entity<MaterialRequest>().HasKey(mr => mr.MaterialRequestId);
            modelBuilder.Entity<Invoice>().HasKey(i => i.InvoiceId);
            modelBuilder.Entity<Payment>().HasKey(p => p.PaymentId);

            // Configure decimal precision for monetary values
            modelBuilder.Entity<InventoryItem>()
                .Property(i => i.UnitCost)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<PurchaseOrder>()
                .Property(po => po.TotalAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Invoice>()
                .Property(i => i.Amount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");

            // UPDATE: Match web app delete behaviors exactly
            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(p => p.Supplier)
                .WithMany(s => s.PurchaseOrders)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict); // Match web app

            modelBuilder.Entity<PurchaseOrder>()
                .HasOne(po => po.MaterialRequest)
                .WithMany(mr => mr.PurchaseOrders)
                .HasForeignKey(po => po.MaterialRequestId)
                .OnDelete(DeleteBehavior.Cascade); // Match web app

            // Add basic relationships that are essential
            modelBuilder.Entity<InventoryItem>()
                .HasOne(i => i.Warehouse)
                .WithMany(w => w.InventoryItems)
                .HasForeignKey(i => i.WarehouseId);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.PurchaseOrder)
                .WithMany(po => po.Items)
                .HasForeignKey(oi => oi.PurchaseOrderId);

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Order)
                .WithMany(po => po.Invoices)
                .HasForeignKey(i => i.OrderId);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Invoice)
                .WithMany()
                .HasForeignKey(p => p.InvoiceId);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.ApprovedBy)
                .WithMany()
                .HasForeignKey(p => p.ApprovedById);
        }
    }
}