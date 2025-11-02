using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiningOps.Data;
using MiningOps.Models.Entities;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MiningOps.Services
{
    public class InventoryService
    {
        private readonly DatabaseService _dbService;

        public InventoryService(IConfiguration configuration)
        {
            _dbService = new DatabaseService(configuration);
        }

        // CREATE
        public async Task<InventoryItem> CreateItemAsync(InventoryItem item)
        {
            using var context = _dbService.CreateDbContext();

            if (context == null)
                throw new System.Exception("Database context is null");

            // UPDATED: Use LastUpdated instead of CreatedAt/UpdatedAt
            item.LastUpdated = System.DateTime.UtcNow;

            context.InventoryDb.Add(item);
            await context.SaveChangesAsync();
            return item;
        }

        // READ - All items
        public async Task<List<InventoryItem>> GetAllItemsAsync()
        {
            using var context = _dbService.CreateDbContext();

            if (context == null)
                return new List<InventoryItem>();

            try
            {
                var items = await context.InventoryDb
                    .Include(i => i.Warehouse)
                    .ToListAsync();

                // REMOVED: Null handling since fields are now non-nullable
                Debug.WriteLine($"✅ InventoryService: Loaded {items?.Count ?? 0} items");
                return items ?? new List<InventoryItem>();
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"❌ InventoryService.GetAllItemsAsync failed: {ex.Message}");
                return new List<InventoryItem>();
            }
        }

        // READ - Single item
        public async Task<InventoryItem> GetItemByIdAsync(int id)
        {
            using var context = _dbService.CreateDbContext();

            if (context == null)
                return null;

            try
            {
                var item = await context.InventoryDb
                    .Include(i => i.Warehouse)
                    .FirstOrDefaultAsync(i => i.InventoryId == id);

                // REMOVED: Null value handling
                return item;
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"❌ InventoryService.GetItemByIdAsync failed: {ex.Message}");
                return null;
            }
        }

        // READ - Low stock items
        public async Task<List<InventoryItem>> GetLowStockItemsAsync()
        {
            using var context = _dbService.CreateDbContext();

            if (context == null)
                return new List<InventoryItem>();

            try
            {
                var lowStockItems = await context.InventoryDb
                    .Include(i => i.Warehouse)
                    .Where(i => i.Quantity <= i.ReorderLevel) // UPDATED: No null handling needed
                    .ToListAsync();

                // REMOVED: Null value handling
                Debug.WriteLine($"✅ InventoryService: Loaded {lowStockItems?.Count ?? 0} low stock items");
                return lowStockItems ?? new List<InventoryItem>();
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"❌ InventoryService.GetLowStockItemsAsync failed: {ex.Message}");
                return new List<InventoryItem>();
            }
        }

        // UPDATE
        public async Task<InventoryItem> UpdateItemAsync(InventoryItem item)
        {
            using var context = _dbService.CreateDbContext();

            if (context == null)
                throw new System.Exception("Database context is null");

            // UPDATED: Use LastUpdated instead of UpdatedAt
            item.LastUpdated = System.DateTime.UtcNow;

            context.InventoryDb.Update(item);
            await context.SaveChangesAsync();
            return item;
        }

        // DELETE
        public async Task<bool> DeleteItemAsync(int id)
        {
            using var context = _dbService.CreateDbContext();

            if (context == null)
                return false;

            var item = await context.InventoryDb.FindAsync(id);
            if (item != null)
            {
                context.InventoryDb.Remove(item);
                await context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        // Additional business logic methods
        public async Task<bool> UpdateStockQuantityAsync(int itemId, int newQuantity)
        {
            using var context = _dbService.CreateDbContext();

            if (context == null)
                return false;

            var item = await context.InventoryDb.FindAsync(itemId);
            if (item != null)
            {
                item.Quantity = newQuantity;
                item.LastUpdated = System.DateTime.UtcNow; // UPDATED
                await context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> AdjustStockAsync(int itemId, int quantityChange)
        {
            using var context = _dbService.CreateDbContext();

            if (context == null)
                return false;

            var item = await context.InventoryDb.FindAsync(itemId);
            if (item != null)
            {
                item.Quantity += quantityChange; // UPDATED: No null handling needed
                item.LastUpdated = System.DateTime.UtcNow; // UPDATED
                await context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}