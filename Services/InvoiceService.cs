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
    public class InvoiceService
    {
        private readonly DatabaseService _dbService;

        public InvoiceService(IConfiguration configuration)
        {
            _dbService = new DatabaseService(configuration);
        }

        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            return await _dbService.ExecuteSafeAsync(async context =>
            {
                try
                {
                    // PHASE 1: Try EF Core with safe entity mapping first
                    var invoices = await context.InvoicesDb
                        .Include(i => i.Order)
                        .ThenInclude(o => o.Supplier)
                        .ThenInclude(s => s.RegisterMining)
                        .Include(i => i.Order)
                        .ThenInclude(o => o.Requester)
                        .AsNoTracking()
                        .ToListAsync();

                    // Apply null safety to each invoice
                    foreach (var invoice in invoices)
                    {
                        EnsureInvoiceNullSafety(invoice);
                    }

                    Debug.WriteLine($"✅ EF Core loading successful: {invoices.Count} invoices");
                    return invoices;
                }
                catch (Exception efEx)
                {
                    Debug.WriteLine($"⚠️ EF Core failed, falling back to raw SQL: {efEx.Message}");

                    // PHASE 2: Fallback to safe raw SQL
                    return await LoadInvoicesWithSafeRawSql(context);
                }
            }, new List<Invoice>());
        }

        private async Task<List<Invoice>> LoadInvoicesWithSafeRawSql(AppDbContext context)
        {
            try
            {
                // ✅ SAFE: Parameter-less raw SQL (no injection risk)
                var sql = @"
                SELECT 
                    InvoiceId,
                    OrderId,
                    InvoiceDate,
                    ISNULL(DueDate, GETUTCDATE()) as DueDate,
                    ISNULL(Amount, 0) as Amount,
                    ISNULL(Status, 0) as Status,
                    ISNULL(InvoiceReference, 'REF-' + CONVERT(VARCHAR, InvoiceId)) as InvoiceReference,
                    ISNULL(InvoiceFilePath, '') as InvoiceFilePath
                FROM InvoicesDb";

                var invoices = await context.InvoicesDb
                    .FromSqlRaw(sql)
                    .AsNoTracking()
                    .ToListAsync();

                // Apply null safety
                foreach (var invoice in invoices)
                {
                    EnsureInvoiceNullSafety(invoice);
                }

                Debug.WriteLine($"✅ Raw SQL fallback successful: {invoices.Count} invoices");
                return invoices;
            }
            catch (Exception sqlEx)
            {
                Debug.WriteLine($"❌ Raw SQL fallback also failed: {sqlEx.Message}");
                return new List<Invoice>();
            }
        }
        public async Task<bool> UpdateInvoiceStatusAsync(int invoiceId, InvoiceStatus newStatus)
        {
            return await _dbService.ExecuteSafeAsync(async context =>
            {
                try
                {
                    var invoice = await context.InvoicesDb.FindAsync(invoiceId);
                    if (invoice != null)
                    {
                        invoice.Status = newStatus;
                        await context.SaveChangesAsync();
                        Debug.WriteLine($"✅ InvoiceService: Updated invoice {invoiceId} status to {newStatus}");
                        return true;
                    }
                    Debug.WriteLine($"⚠️ InvoiceService: Invoice {invoiceId} not found for status update");
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Error updating invoice status {invoiceId}: {ex.Message}");
                    throw new Exception($"Failed to update invoice status {invoiceId}: {ex.Message}", ex);
                }
            }, false);
        }
        private void EnsureInvoiceNullSafety(Invoice invoice)
        {
            // Ensure all properties have safe values
            invoice.DueDate = invoice.DueDate ?? DateTime.UtcNow.AddDays(30);
            invoice.Amount = invoice.Amount ?? 0m;
            invoice.Status = invoice.Status ?? InvoiceStatus.Unpaid;
            invoice.InvoiceReference = invoice.InvoiceReference ?? $"INV-{invoice.InvoiceId:D6}";
            invoice.InvoiceFilePath = invoice.InvoiceFilePath ?? string.Empty;
        }

        // Enhanced individual methods using the safe execution pattern
        public async Task<Invoice> GetInvoiceByIdAsync(int id)
        {
            return await _dbService.ExecuteSafeAsync(async context =>
            {
                var invoice = await context.InvoicesDb
                    .Include(i => i.Order)
                    .ThenInclude(o => o.Supplier)
                    .FirstOrDefaultAsync(i => i.InvoiceId == id);

                if (invoice != null)
                {
                    EnsureInvoiceNullSafety(invoice);
                }

                return invoice;
            });
        }

        public async Task<List<Invoice>> GetOverdueInvoicesAsync()
        {
            return await _dbService.ExecuteSafeAsync(async context =>
            {
                var overdueInvoices = await context.InvoicesDb
                    .Where(i => i.DueDate < DateTime.UtcNow &&
                               (i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.Partial))
                    .Include(i => i.Order)
                    .ThenInclude(o => o.Supplier)
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var invoice in overdueInvoices)
                {
                    EnsureInvoiceNullSafety(invoice);
                }

                return overdueInvoices;
            }, new List<Invoice>());
        }

        // CRUD operations with enhanced error handling
        public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
        {
            using var context = _dbService.CreateDbContext();

            try
            {
                // Ensure required fields before saving
                invoice.InvoiceDate = DateTime.UtcNow;
                invoice.Amount = invoice.Amount ?? 0m;
                invoice.Status = invoice.Status ?? InvoiceStatus.Unpaid;
                invoice.InvoiceReference = invoice.InvoiceReference ??
                    $"INV-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

                context.InvoicesDb.Add(invoice);
                await context.SaveChangesAsync();
                return invoice;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ Error creating invoice: {ex.Message}");
                throw new Exception($"Failed to create invoice: {ex.Message}", ex);
            }
        }

        // Status mapping for web app compatibility
        private InvoiceStatus MapWebAppStatus(int webAppStatus)
        {
            // Map web app status to your WPF enum
            return webAppStatus switch
            {
                0 => InvoiceStatus.Unpaid,      // Web: Unpaid
                1 => InvoiceStatus.Paid,        // Web: Paid
                2 => InvoiceStatus.Overdue,     // Web: Overdue  
                3 => InvoiceStatus.Partial,     // Web: PartiallyPaid
                _ => InvoiceStatus.Unpaid
            };
        }

        private int MapToWebAppStatus(InvoiceStatus wpfStatus)
        {
            // Map your WPF enum to web app status
            return wpfStatus switch
            {
                InvoiceStatus.Unpaid => 0,
                InvoiceStatus.Paid => 1,
                InvoiceStatus.Overdue => 2,
                InvoiceStatus.Partial => 3,
                _ => 0
            };
        }
    }
}