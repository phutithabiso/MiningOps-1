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
    public class RequestService
    {
        private readonly DatabaseService _dbService;

        public RequestService(IConfiguration configuration)
        {
            _dbService = new DatabaseService(configuration);
        }

        public async Task<List<MaterialRequest>> GetAllRequestsAsync()
        {
            try
            {
                using var context = _dbService.CreateDbContext();
                var requests = await context.MaterialRequestsDb
                    .Include(mr => mr.PurchaseOrders)
                    .ThenInclude(po => po.Supplier)
                    .ToListAsync() ?? new List<MaterialRequest>();

                // REMOVED: Null handling since Quantity is now non-nullable
                return requests;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ RequestService.GetAllRequestsAsync failed: {ex.Message}");
                return new List<MaterialRequest>();
            }
        }

        public async Task<MaterialRequest> GetRequestByIdAsync(int id)
        {
            try
            {
                using var context = _dbService.CreateDbContext();
                var request = await context.MaterialRequestsDb
                    .Include(mr => mr.PurchaseOrders)
                    .ThenInclude(po => po.Supplier)
                    .FirstOrDefaultAsync(mr => mr.MaterialRequestId == id);

                // REMOVED: Null handling
                return request;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ RequestService.GetRequestByIdAsync failed: {ex.Message}");
                return null;
            }
        }

        public async Task<MaterialRequest> CreateRequestAsync(MaterialRequest request)
        {
            try
            {
                using var context = _dbService.CreateDbContext();
                request.RequestDate = DateTime.UtcNow;
                // REMOVED: Safe value assignment
                context.MaterialRequestsDb.Add(request);
                await context.SaveChangesAsync();
                return request;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ RequestService.CreateRequestAsync failed: {ex.Message}");
                throw new Exception($"Error creating request: {ex.Message}", ex);
            }
        }

        public async Task<MaterialRequest> UpdateRequestAsync(MaterialRequest request)
        {
            try
            {
                using var context = _dbService.CreateDbContext();
                // REMOVED: Safe value assignment
                context.MaterialRequestsDb.Update(request);
                await context.SaveChangesAsync();
                return request;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ RequestService.UpdateRequestAsync failed: {ex.Message}");
                throw new Exception($"Error updating request: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteRequestAsync(int id)
        {
            try
            {
                using var context = _dbService.CreateDbContext();
                var request = await context.MaterialRequestsDb.FindAsync(id);
                if (request != null)
                {
                    context.MaterialRequestsDb.Remove(request);
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ RequestService.DeleteRequestAsync failed: {ex.Message}");
                throw new Exception($"Error deleting request {id}: {ex.Message}", ex);
            }
        }

        public async Task<List<MaterialRequest>> GetRequestsByStatusAsync(string status)
        {
            try
            {
                using var context = _dbService.CreateDbContext();
                var requests = await context.MaterialRequestsDb
                    .Include(mr => mr.PurchaseOrders)
                    .Where(mr => mr.Status == status)
                    .ToListAsync() ?? new List<MaterialRequest>();

                return requests;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ RequestService.GetRequestsByStatusAsync failed: {ex.Message}");
                return new List<MaterialRequest>();
            }
        }

        public async Task<bool> ApproveRequestAsync(int requestId)
        {
            try
            {
                using var context = _dbService.CreateDbContext();
                var request = await context.MaterialRequestsDb.FindAsync(requestId);
                if (request != null)
                {
                    request.Status = "Approved";
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ RequestService.ApproveRequestAsync failed: {ex.Message}");
                throw new Exception($"Error approving request {requestId}: {ex.Message}", ex);
            }
        }

        public async Task<bool> RejectRequestAsync(int requestId, string notes = null)
        {
            try
            {
                using var context = _dbService.CreateDbContext();
                var request = await context.MaterialRequestsDb.FindAsync(requestId);
                if (request != null)
                {
                    request.Status = "Rejected";
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ RequestService.RejectRequestAsync failed: {ex.Message}");
                throw new Exception($"Error rejecting request {requestId}: {ex.Message}", ex);
            }
        }

        public async Task<List<MaterialRequest>> GetPendingRequestsAsync()
        {
            return await GetRequestsByStatusAsync("Pending");
        }

        public async Task<List<MaterialRequest>> GetApprovedRequestsAsync()
        {
            return await GetRequestsByStatusAsync("Approved");
        }

        public async Task<int> GetPendingRequestsCountAsync()
        {
            try
            {
                using var context = _dbService.CreateDbContext();
                return await context.MaterialRequestsDb
                    .CountAsync(mr => mr.Status == "Pending");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"❌ RequestService.GetPendingRequestsCountAsync failed: {ex.Message}");
                return 0;
            }
        }
    }
}