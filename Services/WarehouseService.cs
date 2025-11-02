using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiningOps.Data;
using MiningOps.Models.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiningOps.Services
{
    public class WarehouseService
    {
        private readonly DatabaseService _dbService;

        public WarehouseService(IConfiguration configuration)
        {
            _dbService = new DatabaseService(configuration);
        }

        public async Task<List<Warehouse>> GetAllWarehousesAsync()
        {
            using var context = _dbService.CreateDbContext();
            return await context.WarehousesDb
                .Include(w => w.InventoryItems)
                .ToListAsync();
        }

        public async Task<Warehouse> GetWarehouseByIdAsync(int id)
        {
            using var context = _dbService.CreateDbContext();
            return await context.WarehousesDb
                .Include(w => w.InventoryItems)
                .FirstOrDefaultAsync(w => w.WarehouseId == id);
        }

        public async Task<Warehouse> CreateWarehouseAsync(Warehouse warehouse)
        {
            using var context = _dbService.CreateDbContext();
            warehouse.CreatedAt = DateTime.UtcNow;
            context.WarehousesDb.Add(warehouse);
            await context.SaveChangesAsync();
            return warehouse;
        }

        public async Task<Warehouse> UpdateWarehouseAsync(Warehouse warehouse)
        {
            using var context = _dbService.CreateDbContext();
            context.WarehousesDb.Update(warehouse);
            await context.SaveChangesAsync();
            return warehouse;
        }

        public async Task<bool> DeleteWarehouseAsync(int id)
        {
            using var context = _dbService.CreateDbContext();
            var warehouse = await context.WarehousesDb.FindAsync(id);
            if (warehouse != null)
            {
                context.WarehousesDb.Remove(warehouse);
                await context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<List<Warehouse>> GetWarehousesWithLowStockAsync()
        {
            using var context = _dbService.CreateDbContext();
            return await context.WarehousesDb
                .Include(w => w.InventoryItems)
                .Where(w => w.InventoryItems.Any(i => i.Quantity <= i.ReorderLevel))
                .ToListAsync();
        }
    }
}