using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiningOps.Data;
using MiningOps.Models.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiningOps.Services
{
    public class SupplierService
    {
        private readonly DatabaseService _dbService;

        public SupplierService(IConfiguration configuration)
        {
            _dbService = new DatabaseService(configuration);
        }

        public async Task<List<Supplier>> GetAllSuppliersAsync()
        {
            using var context = _dbService.CreateDbContext();
            return await context.SupplierProfiles
                .Include(s => s.RegisterMining)
                .ToListAsync();
        }

        public async Task<Supplier> GetSupplierByIdAsync(int id)
        {
            using var context = _dbService.CreateDbContext();
            return await context.SupplierProfiles
                .Include(s => s.RegisterMining)
                .FirstOrDefaultAsync(s => s.SupplierId == id);
        }

        public async Task<Supplier> GetSupplierByAccountIdAsync(int accId)
        {
            using var context = _dbService.CreateDbContext();
            return await context.SupplierProfiles
                .Include(s => s.RegisterMining)
                .FirstOrDefaultAsync(s => s.AccId == accId);
        }

        public async Task<Supplier> CreateSupplierAsync(Supplier supplier)
        {
            using var context = _dbService.CreateDbContext();

            // Verify the user exists and has Supplier role
            var user = await context.RegisterMiningDb
                .FirstOrDefaultAsync(u => u.AccId == supplier.AccId);

            if (user == null)
            {
                throw new Exception("Selected user does not exist.");
            }

            // Ensure the user has Supplier role
            if (user.Role != UserRole.Supplier)
            {
                throw new Exception("Selected user must have Supplier role.");
            }

            // Check if user already has a supplier profile
            var existingSupplier = await context.SupplierProfiles
                .FirstOrDefaultAsync(s => s.AccId == supplier.AccId);

            if (existingSupplier != null)
            {
                throw new Exception("This user already has a supplier profile.");
            }

            context.SupplierProfiles.Add(supplier);
            await context.SaveChangesAsync();
            return supplier;
        }

        public async Task<Supplier> UpdateSupplierAsync(Supplier supplier)
        {
            using var context = _dbService.CreateDbContext();
            context.SupplierProfiles.Update(supplier);
            await context.SaveChangesAsync();
            return supplier;
        }

        public async Task<bool> DeleteSupplierAsync(int id)
        {
            using var context = _dbService.CreateDbContext();
            var supplier = await context.SupplierProfiles.FindAsync(id);
            if (supplier != null)
            {
                context.SupplierProfiles.Remove(supplier);
                await context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<Supplier>> GetActiveSuppliersAsync()
        {
            using var context = _dbService.CreateDbContext();
            return await context.SupplierProfiles
                .Include(s => s.RegisterMining)
                .Where(s => s.RegisterMining.UpdatedAt == null) // Active suppliers
                .ToListAsync();
        }
    }
}