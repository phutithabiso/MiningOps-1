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
    public class PaymentService
    {
        private readonly DatabaseService _dbService;

        public PaymentService(IConfiguration configuration)
        {
            _dbService = new DatabaseService(configuration);
        }

        public async Task<List<Payment>> GetAllPaymentsAsync()
        {
            return await _dbService.ExecuteSafeAsync(async context =>
            {
                try
                {
                    // Try EF Core first
                    var payments = await context.PaymentsDb
                        .Include(p => p.Invoice)
                        .ThenInclude(i => i.Order)
                        .Include(p => p.ApprovedBy)
                        .AsNoTracking()
                        .ToListAsync();

                    foreach (var payment in payments)
                    {
                        EnsurePaymentNullSafety(payment);
                    }

                    Debug.WriteLine($"✅ PaymentService: Loaded {payments.Count} payments via EF Core");
                    return payments;
                }
                catch (Exception efEx)
                {
                    Debug.WriteLine($"⚠️ EF Core failed for payments, using raw SQL: {efEx.Message}");
                    return await LoadPaymentsWithSafeRawSql(context);
                }
            }, new List<Payment>());
        }

        private async Task<List<Payment>> LoadPaymentsWithSafeRawSql(AppDbContext context)
        {
            try
            {
                var sql = @"
                    SELECT 
                        PaymentId,
                        InvoiceId,
                        ISNULL(Amount, 0) as Amount,
                        ISNULL(PaymentReference, 'PAY-' + CONVERT(VARCHAR, PaymentId)) as PaymentReference,
                        PaidDate,
                        ApprovedById
                    FROM PaymentsDb";

                var payments = await context.PaymentsDb
                    .FromSqlRaw(sql)
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var payment in payments)
                {
                    EnsurePaymentNullSafety(payment);
                }

                Debug.WriteLine($"✅ PaymentService: Loaded {payments.Count} payments via raw SQL");
                return payments;
            }
            catch (Exception sqlEx)
            {
                Debug.WriteLine($"❌ Raw SQL for payments failed: {sqlEx.Message}");
                return new List<Payment>();
            }
        }

        private void EnsurePaymentNullSafety(Payment payment)
        {
            payment.Amount = payment.Amount ?? 0m;
            payment.PaymentReference = payment.PaymentReference ?? $"PAY-{payment.PaymentId:D6}";
            payment.PaidDate = (payment.PaidDate == DateTime.MinValue || payment.PaidDate == default)
                ? DateTime.UtcNow
                : payment.PaidDate;
        }

        public async Task<Payment> GetPaymentByIdAsync(int id)
        {
            return await _dbService.ExecuteSafeAsync(async context =>
            {
                try
                {
                    // Try EF Core first
                    var payment = await context.PaymentsDb
                        .Include(p => p.Invoice)
                        .ThenInclude(i => i.Order)
                        .Include(p => p.ApprovedBy)
                        .FirstOrDefaultAsync(p => p.PaymentId == id);

                    if (payment != null)
                    {
                        EnsurePaymentNullSafety(payment);
                    }

                    return payment;
                }
                catch (Exception efEx)
                {
                    Debug.WriteLine($"⚠️ EF Core failed for GetPaymentById, using raw SQL: {efEx.Message}");

                    // Fallback to raw SQL
                    var sql = @"
                        SELECT 
                            PaymentId,
                            InvoiceId,
                            ISNULL(Amount, 0) as Amount,
                            ISNULL(PaymentReference, 'PAY-' + CONVERT(VARCHAR, PaymentId)) as PaymentReference,
                            PaidDate,
                            ApprovedById
                        FROM PaymentsDb 
                        WHERE PaymentId = {0}";

                    var payment = await context.PaymentsDb
                        .FromSqlRaw(sql, id)
                        .Include(p => p.Invoice)
                        .Include(p => p.ApprovedBy)
                        .FirstOrDefaultAsync();

                    if (payment != null)
                    {
                        EnsurePaymentNullSafety(payment);
                    }

                    return payment;
                }
            });
        }

        public async Task<Payment> CreatePaymentAsync(Payment payment)
        {
            return await _dbService.ExecuteSafeAsync(async context =>
            {
                try
                {
                    // Enhanced null safety before saving
                    payment.PaidDate = DateTime.UtcNow;
                    payment.Amount = payment.Amount ?? 0m;
                    payment.PaymentReference = payment.PaymentReference ??
                        $"PAY-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

                    context.PaymentsDb.Add(payment);
                    await context.SaveChangesAsync();

                    Debug.WriteLine($"✅ PaymentService: Created payment {payment.PaymentId}");
                    return payment;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Error creating payment: {ex.Message}");
                    throw new Exception($"Failed to create payment: {ex.Message}", ex);
                }
            });
        }

        public async Task<Payment> UpdatePaymentAsync(Payment payment)
        {
            return await _dbService.ExecuteSafeAsync(async context =>
            {
                try
                {
                    // Ensure null safety before update
                    EnsurePaymentNullSafety(payment);

                    context.PaymentsDb.Update(payment);
                    await context.SaveChangesAsync();

                    Debug.WriteLine($"✅ PaymentService: Updated payment {payment.PaymentId}");
                    return payment;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Error updating payment: {ex.Message}");
                    throw new Exception($"Failed to update payment: {ex.Message}", ex);
                }
            });
        }

        public async Task<bool> DeletePaymentAsync(int id)
        {
            return await _dbService.ExecuteSafeAsync(async context =>
            {
                try
                {
                    var payment = await context.PaymentsDb.FindAsync(id);
                    if (payment != null)
                    {
                        context.PaymentsDb.Remove(payment);
                        await context.SaveChangesAsync();
                        Debug.WriteLine($"✅ PaymentService: Deleted payment {id}");
                        return true;
                    }
                    Debug.WriteLine($"⚠️ PaymentService: Payment {id} not found for deletion");
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Error deleting payment {id}: {ex.Message}");
                    throw new Exception($"Failed to delete payment {id}: {ex.Message}", ex);
                }
            }, false);
        }

        public async Task<bool> ApprovePaymentAsync(int paymentId, int approvedById)
        {
            return await _dbService.ExecuteSafeAsync(async context =>
            {
                try
                {
                    var payment = await context.PaymentsDb.FindAsync(paymentId);
                    if (payment != null)
                    {
                        payment.ApprovedById = approvedById;
                        await context.SaveChangesAsync();
                        Debug.WriteLine($"✅ PaymentService: Approved payment {paymentId}");
                        return true;
                    }
                    Debug.WriteLine($"⚠️ PaymentService: Payment {paymentId} not found for approval");
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"❌ Error approving payment {paymentId}: {ex.Message}");
                    throw new Exception($"Failed to approve payment {paymentId}: {ex.Message}", ex);
                }
            }, false);
        }
    }
}