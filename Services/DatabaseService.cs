using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiningOps.Data;
using System;
using System.Threading.Tasks;

namespace MiningOps.Services
{
    public class DatabaseService
    {
        private readonly IConfiguration _configuration;

        public DatabaseService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public AppDbContext CreateDbContext()
        {
            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Database connection string is null or empty.");
                }

                optionsBuilder.UseSqlServer(connectionString, options =>
                {
                    options.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                    options.CommandTimeout(30);
                });

                return new AppDbContext(optionsBuilder.Options);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create DbContext: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var context = CreateDbContext();
                var canConnect = await context.Database.CanConnectAsync();

                if (canConnect)
                {
                    System.Diagnostics.Debug.WriteLine("Database connection successful.");
                    return true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Database connection failed.");
                    return false;
                }
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx)
            {
                System.Diagnostics.Debug.WriteLine($"SQL Connection Error: {sqlEx.Message}");
                System.Diagnostics.Debug.WriteLine($"Error Number: {sqlEx.Number}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database connection test failed: {ex.Message}");
                return false;
            }
        }

        // NEW: Safe execution wrapper for all database operations
        public async Task<T> ExecuteSafeAsync<T>(Func<AppDbContext, Task<T>> operation, T defaultValue = default(T))
        {
            try
            {
                using var context = CreateDbContext();
                return await operation(context);
            }
            catch (Microsoft.Data.SqlClient.SqlException sqlEx)
            {
                System.Diagnostics.Debug.WriteLine($"SQL Error in ExecuteSafeAsync: {sqlEx.Message}");
                return defaultValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database operation failed in ExecuteSafeAsync: {ex.Message}");
                return defaultValue;
            }
        }
    }
}