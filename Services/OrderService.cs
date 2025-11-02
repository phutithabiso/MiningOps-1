using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiningOps.Data;
using MiningOps.Models.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace MiningOps.Services
{
    public class OrderService
    {
        private readonly DatabaseService _dbService;

        public OrderService(IConfiguration configuration)
        {
            _dbService = new DatabaseService(configuration);
        }

        public async Task<List<PurchaseOrder>> GetAllOrdersAsync()
        {
            try
            {
                using var context = _dbService.CreateDbContext();
                var orders = await context.PurchaseOrdersDb
                    .Include(po => po.Supplier)
                    .ThenInclude(s => s.RegisterMining)
                    .Include(po => po.Requester)
                    .Include(po => po.Items)
                    .Include(po => po.Invoices)
                    .ToListAsync() ?? new List<PurchaseOrder>();

                // REMOVED: Null handling since fields are now non-nullable
                return orders;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ OrderService.GetAllOrdersAsync failed: {ex.Message}");
                return new List<PurchaseOrder>();
            }
        }

        public async Task<PurchaseOrder> GetOrderByIdAsync(int id)
        {
            try
            {
                using var context = _dbService.CreateDbContext();
                var order = await context.PurchaseOrdersDb
                    .Include(po => po.Supplier)
                    .ThenInclude(s => s.RegisterMining)
                    .Include(po => po.Requester)
                    .Include(po => po.Items)
                    .Include(po => po.Invoices)
                    .FirstOrDefaultAsync(po => po.OrderId == id);

                // REMOVED: Null handling
                return order;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ OrderService.GetOrderByIdAsync failed: {ex.Message}");
                return null;
            }
        }

        public async Task<PurchaseOrder> CreateOrderAsync(PurchaseOrder order)
        {
            try
            {
                using var context = _dbService.CreateDbContext();
                order.CreatedAt = DateTime.UtcNow;
                // REMOVED: Safe value assignment
                context.PurchaseOrdersDb.Add(order);
                await context.SaveChangesAsync();
                return order;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ OrderService.CreateOrderAsync failed: {ex.Message}");
                throw new Exception($"Error creating order: {ex.Message}", ex);
            }
        }

        public async Task<PurchaseOrder> UpdateOrderAsync(PurchaseOrder order)
        {
            try
            {
                using var context = _dbService.CreateDbContext();
                // REMOVED: Safe value assignment
                context.PurchaseOrdersDb.Update(order);
                await context.SaveChangesAsync();
                return order;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ OrderService.UpdateOrderAsync failed: {ex.Message}");
                throw new Exception($"Error updating order: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            try
            {
                using var context = _dbService.CreateDbContext();
                var order = await context.PurchaseOrdersDb.FindAsync(id);
                if (order != null)
                {
                    context.PurchaseOrdersDb.Remove(order);
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ OrderService.DeleteOrderAsync failed: {ex.Message}");
                throw new Exception($"Error deleting order {id}: {ex.Message}", ex);
            }
        }

        public async Task<List<PurchaseOrder>> GetOrdersByStatusAsync(OrderStatus status)
        {
            try
            {
                using var context = _dbService.CreateDbContext();
                var orders = await context.PurchaseOrdersDb
                    .Include(po => po.Supplier)
                    .ThenInclude(s => s.RegisterMining)
                    .Include(po => po.Requester)
                    .Where(po => po.Status == status)
                    .ToListAsync() ?? new List<PurchaseOrder>();

                return orders;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ OrderService.GetOrdersByStatusAsync failed: {ex.Message}");
                return new List<PurchaseOrder>();
            }
        }

        public async Task<List<PurchaseOrder>> GetOrdersBySupplierAsync(int supplierId)
        {
            try
            {
                using var context = _dbService.CreateDbContext();
                var orders = await context.PurchaseOrdersDb
                    .Include(po => po.Supplier)
                    .ThenInclude(s => s.RegisterMining)
                    .Include(po => po.Requester)
                    .Include(po => po.Items)
                    .Where(po => po.SupplierId == supplierId)
                    .ToListAsync() ?? new List<PurchaseOrder>();

                return orders;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ OrderService.GetOrdersBySupplierAsync failed: {ex.Message}");
                return new List<PurchaseOrder>();
            }
        }
        // Add this method to your OrderService class
        public async Task<List<PurchaseOrder>> GetOrdersByRequesterAsync(int requestedBy)
        {
            try
            {
                using var context = _dbService.CreateDbContext();
                return await context.PurchaseOrdersDb
                    .Include(po => po.Supplier)
                    .ThenInclude(s => s.RegisterMining)
                    .Include(po => po.Requester)
                    .Include(po => po.Items)
                    .Include(po => po.Invoices)
                    .Where(po => po.RequestedBy == requestedBy)
                    .ToListAsync() ?? new List<PurchaseOrder>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ OrderService.GetOrdersByRequesterAsync failed: {ex.Message}");
                return new List<PurchaseOrder>();
            }
        }
        public async Task<PurchaseOrder> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            try
            {
                using var context = _dbService.CreateDbContext();
                var order = await context.PurchaseOrdersDb.FindAsync(orderId);
                if (order != null)
                {
                    order.Status = newStatus;
                    await context.SaveChangesAsync();
                }
                return order;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ OrderService.UpdateOrderStatusAsync failed: {ex.Message}");
                throw new Exception($"Error updating order status {orderId}: {ex.Message}", ex);
            }
        }

        public async Task<decimal> CalculateOrderTotalAsync(int orderId)
        {
            try
            {
                using var context = _dbService.CreateDbContext();
                var order = await context.PurchaseOrdersDb
                    .Include(po => po.Items)
                    .FirstOrDefaultAsync(po => po.OrderId == orderId);

                if (order?.Items == null)
                    return 0m;

                // UPDATED: Direct calculation without safe methods
                return order.Items.Sum(item => item.Quantity * item.UnitPrice);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ OrderService.CalculateOrderTotalAsync failed: {ex.Message}");
                return 0m;
            }
        }

        public async Task<Invoice> CreateInvoiceFromOrderAsync(int orderId)
        {
            try
            {
                using var context = _dbService.CreateDbContext();

                var order = await context.PurchaseOrdersDb
                    .Include(po => po.Supplier)
                    .Include(po => po.Items)
                    .FirstOrDefaultAsync(po => po.OrderId == orderId);

                if (order == null)
                    throw new Exception("Order not found");

                // Check if invoice already exists
                var existingInvoice = await context.InvoicesDb
                    .FirstOrDefaultAsync(i => i.OrderId == orderId);

                if (existingInvoice != null)
                    return existingInvoice;

                // Create new invoice
                var invoice = new Invoice
                {
                    OrderId = orderId,
                    InvoiceDate = DateTime.UtcNow,
                    DueDate = DateTime.UtcNow.AddDays(30),
                    Amount = order.TotalAmount, // UPDATED: Direct assignment
                    Status = InvoiceStatus.Unpaid,
                    InvoiceReference = $"INV-{orderId}-{DateTime.UtcNow:yyyyMMdd}"
                };

                context.InvoicesDb.Add(invoice);
                await context.SaveChangesAsync();

                return invoice;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ OrderService.CreateInvoiceFromOrderAsync failed: {ex.Message}");
                throw new Exception($"Error creating invoice for order {orderId}: {ex.Message}", ex);
            }
        }
    }
}