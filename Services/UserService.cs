using Microsoft.Extensions.Configuration;
using MiningOps.Models.Entities;
using MiningOps.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using MiningOps.Security;

namespace MiningOps.Services
{
    public class UserService
    {
        private readonly IConfiguration _configuration;
        private readonly DatabaseService _databaseService;

        public UserService(IConfiguration configuration, DatabaseService databaseService)
        {
            _configuration = configuration;
            _databaseService = databaseService;
            Debug.WriteLine("👥 UserService initialized with DatabaseService");
        }

        public async Task<bool> ValidateUserAsync(string usernameOrEmail, string password)
        {
            try
            {
                Debug.WriteLine($"🔐 Validating user: {usernameOrEmail}");

                var user = await GetUserByUsernameOrEmailAsync(usernameOrEmail);
                if (user == null)
                {
                    Debug.WriteLine($"❌ User not found: {usernameOrEmail}");
                    return false;
                }

                // Check if user is active (UpdatedAt is null for active users)
                if (user.UpdatedAt != null)
                {
                    Debug.WriteLine($"❌ User is inactive: {usernameOrEmail}");
                    return false;
                }

                // ✅ UPDATED: Use SHA256 hashing to match web app
                var isValid = PasswordHasher.VerifyPassword(password, user.PasswordHash, user.Salt);
                Debug.WriteLine($"🔐 Password validation result for {usernameOrEmail}: {isValid}");

                if (isValid)
                {
                    Debug.WriteLine($"✅ User {usernameOrEmail} authenticated successfully");
                }
                else
                {
                    Debug.WriteLine($"❌ Invalid password for user: {usernameOrEmail}");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 Error validating user {usernameOrEmail}: {ex.Message}");
                Debug.WriteLine($"💥 Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<RegisterMining?> GetUserByUsernameOrEmailAsync(string usernameOrEmail)
        {
            return await _databaseService.ExecuteSafeAsync(async context =>
            {
                // Check both username and email (case-insensitive)
                var user = await context.RegisterMiningDb
                    .Where(u => (u.Username.ToLower() == usernameOrEmail.ToLower() ||
                                u.Email.ToLower() == usernameOrEmail.ToLower()) &&
                                u.UpdatedAt == null) // Only active users
                    .FirstOrDefaultAsync();

                if (user != null)
                {
                    Debug.WriteLine($"✅ Found user: {usernameOrEmail} (Username: {user.Username}, Email: {user.Email}, Role: {user.Role}, AccId: {user.AccId})");
                }
                else
                {
                    Debug.WriteLine($"❌ User not found: {usernameOrEmail}");
                }

                return user;
            });
        }

        public async Task<RegisterMining?> GetUserByUsernameAsync(string username)
        {
            return await _databaseService.ExecuteSafeAsync(async context =>
            {
                var user = await context.RegisterMiningDb
                    .Where(u => u.Username.ToLower() == username.ToLower() &&
                                u.UpdatedAt == null)
                    .FirstOrDefaultAsync();
                return user;
            });
        }

        public async Task<RegisterMining?> GetUserByEmailAsync(string email)
        {
            return await _databaseService.ExecuteSafeAsync(async context =>
            {
                var user = await context.RegisterMiningDb
                    .Where(u => u.Email.ToLower() == email.ToLower() &&
                                u.UpdatedAt == null)
                    .FirstOrDefaultAsync();
                return user;
            });
        }

        public async Task<bool> AdminUserExistsAsync()
        {
            return await _databaseService.ExecuteSafeAsync(async context =>
            {
                var adminExists = await context.RegisterMiningDb
                    .AnyAsync(u => u.Role == UserRole.Admin && u.UpdatedAt == null);

                Debug.WriteLine($"🔍 Admin user exists check: {adminExists}");
                return adminExists;
            }, false);
        }

        public async Task<bool> CreateDefaultAdminUserAsync()
        {
            try
            {
                // Check if admin already exists
                if (await AdminUserExistsAsync())
                {
                    Debug.WriteLine("ℹ️ Admin user already exists, skipping creation");
                    return true;
                }

                Debug.WriteLine("👑 Creating default admin user...");

                var adminUser = new RegisterMining
                {
                    Username = "admin",
                    FullName = "System Administrator",
                    Email = "admin@miningops.com",
                    PhoneNumber = "555-0001",
                    Role = UserRole.Admin,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = null
                };

                await CreateUserAsync(adminUser, "admin123");
                Debug.WriteLine("✅ Default admin user created successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 Error creating default admin user: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RecoverAdminUserAsync()
        {
            return await _databaseService.ExecuteSafeAsync(async context =>
            {
                try
                {
                    // Find any admin user (even if UpdatedAt is set)
                    var adminUser = await context.RegisterMiningDb
                        .Where(u => u.Role == UserRole.Admin)
                        .OrderByDescending(u => u.AccId)
                        .FirstOrDefaultAsync();

                    if (adminUser != null)
                    {
                        // Reset UpdatedAt to null to make it active again
                        adminUser.UpdatedAt = null;

                        // ✅ UPDATED: Use SHA256 hashing to match web app
                        adminUser.Salt = PasswordHasher.GenerateSalt();
                        adminUser.PasswordHash = PasswordHasher.HashPassword("admin123", adminUser.Salt);

                        await context.SaveChangesAsync();
                        Debug.WriteLine($"✅ Recovered admin user: {adminUser.Username} (AccId: {adminUser.AccId})");
                        return true;
                    }

                    Debug.WriteLine("❌ No admin user found to recover");
                    return false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"💥 Error recovering admin user: {ex.Message}");
                    return false;
                }
            }, false);
        }

        public async Task<Supplier?> GetSupplierByAccIdAsync(int accId)
        {
            return await _databaseService.ExecuteSafeAsync(async context =>
            {
                var supplier = await context.SupplierProfiles
                    .FirstOrDefaultAsync(s => s.AccId == accId);
                return supplier;
            });
        }

        public async Task<Supplier?> GetSupplierProfileAsync(int accId)
        {
            return await _databaseService.ExecuteSafeAsync(async context =>
            {
                var supplier = await context.SupplierProfiles
                    .FirstOrDefaultAsync(s => s.AccId == accId);

                if (supplier != null)
                {
                    Debug.WriteLine($"✅ Found supplier profile for AccId {accId}: {supplier.CompanyName}");
                }
                else
                {
                    Debug.WriteLine($"❌ No supplier profile found for AccId {accId}");
                }

                return supplier;
            });
        }

        public async Task CreateUserAsync(RegisterMining user, string password)
        {
            await _databaseService.ExecuteSafeAsync(async context =>
            {
                // ✅ UPDATED: Use SHA256 hashing to match web app
                user.Salt = PasswordHasher.GenerateSalt();
                user.PasswordHash = PasswordHasher.HashPassword(password, user.Salt);

                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = null;

                // Set a default phone number if not provided
                if (string.IsNullOrEmpty(user.PhoneNumber))
                {
                    user.PhoneNumber = "555-0000";
                }

                // Check if username already exists (using ToLower for case-insensitive check)
                var existingUsername = await context.RegisterMiningDb
                    .Where(u => u.Username.ToLower() == user.Username.ToLower())
                    .FirstOrDefaultAsync();

                if (existingUsername != null)
                {
                    Debug.WriteLine($"ℹ️ Username already exists: {user.Username}");
                    throw new InvalidOperationException($"Username '{user.Username}' already exists.");
                }

                // Check if email already exists
                var existingEmail = await context.RegisterMiningDb
                    .Where(u => u.Email.ToLower() == user.Email.ToLower())
                    .FirstOrDefaultAsync();

                if (existingEmail != null)
                {
                    Debug.WriteLine($"ℹ️ Email already exists: {user.Email}");
                    throw new InvalidOperationException($"Email '{user.Email}' already exists.");
                }

                // Prevent multiple admin users
                if (user.Role == UserRole.Admin)
                {
                    var existingAdmin = await context.RegisterMiningDb
                        .Where(u => u.Role == UserRole.Admin && u.UpdatedAt == null)
                        .FirstOrDefaultAsync();

                    if (existingAdmin != null)
                    {
                        Debug.WriteLine($"❌ Admin user already exists: {existingAdmin.Username}");
                        throw new InvalidOperationException("An admin user already exists. Only one admin user is allowed.");
                    }
                }

                // Add user to database
                context.RegisterMiningDb.Add(user);
                await context.SaveChangesAsync();

                Debug.WriteLine($"✅ Created user: {user.Username} (AccId: {user.AccId}, Role: {user.Role})");

                // Create profile based on role
                if (user.Role == UserRole.Supplier)
                {
                    var supplierProfile = new Supplier
                    {
                        AccId = user.AccId,
                        CompanyName = $"{user.FullName} Company",
                        ContactPerson = user.FullName,
                        Address = "123 Business St, City, State 12345",
                        CanViewOrders = true,
                        CanManageInventory = false
                    };

                    context.SupplierProfiles.Add(supplierProfile);
                    Debug.WriteLine($"✅ Created supplier profile for {user.Username}");
                }
                else if (user.Role == UserRole.Admin)
                {
                    var adminProfile = new Admin
                    {
                        AccId = user.AccId,
                        Department = "Administration",
                        CanManageUsers = true,
                        CanApproveRequests = true
                    };

                    context.AdminProfiles.Add(adminProfile);
                    Debug.WriteLine($"✅ Created admin profile for {user.Username}");
                }
                else if (user.Role == UserRole.Supervisor)
                {
                    var supervisorProfile = new Supervisor
                    {
                        AccId = user.AccId,
                        Team = "Mining Team",
                        MineLocation = "Main Mine",
                        Shift = "Day",
                        CanViewReports = true,
                        CanManageTasks = true
                    };

                    context.SupervisorProfiles.Add(supervisorProfile);
                    Debug.WriteLine($"✅ Created supervisor profile for {user.Username}");
                }

                await context.SaveChangesAsync();
                return true;
            }, false);
        }

        public async Task<List<RegisterMining>> GetAllUsersAsync()
        {
            return await _databaseService.ExecuteSafeAsync(async context =>
            {
                return await context.RegisterMiningDb
                    .Where(u => u.UpdatedAt == null) // Only active users
                    .ToListAsync();
            }, new List<RegisterMining>());
        }

        public async Task<bool> UpdateUserPasswordAsync(int accId, string newPassword)
        {
            return await _databaseService.ExecuteSafeAsync(async context =>
            {
                var user = await context.RegisterMiningDb
                    .FirstOrDefaultAsync(u => u.AccId == accId);

                if (user != null)
                {
                    // ✅ UPDATED: Use SHA256 hashing to match web app
                    user.Salt = PasswordHasher.GenerateSalt();
                    user.PasswordHash = PasswordHasher.HashPassword(newPassword, user.Salt);

                    // REMOVED: user.UpdatedAt = DateTime.UtcNow;

                    await context.SaveChangesAsync();
                    Debug.WriteLine($"✅ Password updated for user ID: {accId}");
                    return true;
                }

                Debug.WriteLine($"❌ User not found for password update: {accId}");
                return false;
            }, false);
        }

        public async Task<bool> UpdateUserAsync(RegisterMining user)
        {
            return await _databaseService.ExecuteSafeAsync(async context =>
            {
                var existingUser = await context.RegisterMiningDb
                    .FirstOrDefaultAsync(u => u.AccId == user.AccId);

                if (existingUser != null)
                {
                    // Check if changing to admin role and another admin already exists
                    if (user.Role == UserRole.Admin && existingUser.Role != UserRole.Admin)
                    {
                        var existingAdmin = await context.RegisterMiningDb
                            .Where(u => u.Role == UserRole.Admin && u.AccId != user.AccId && u.UpdatedAt == null)
                            .FirstOrDefaultAsync();

                        if (existingAdmin != null)
                        {
                            throw new InvalidOperationException("An admin user already exists. Only one admin user is allowed.");
                        }
                    }

                    existingUser.FullName = user.FullName;
                    existingUser.Email = user.Email;
                    existingUser.PhoneNumber = user.PhoneNumber;
                    existingUser.Role = user.Role;
                    // REMOVED: existingUser.UpdatedAt = DateTime.UtcNow;

                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }, false);
        }

        public async Task<bool> DeleteUserAsync(int accId)
        {
            return await _databaseService.ExecuteSafeAsync(async context =>
            {
                var user = await context.RegisterMiningDb
                    .FirstOrDefaultAsync(u => u.AccId == accId);

                if (user != null)
                {
                    user.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    return true;
                }
                return false;
            }, false);
        }

        // Test method to verify authentication
        public async Task TestAuthenticationAsync()
        {
            Debug.WriteLine("🧪 Testing authentication...");

            // Test admin by username
            var adminByUsername = await ValidateUserAsync("admin", "admin123");
            Debug.WriteLine($"🧪 Admin by username test: {adminByUsername}");

            // Test admin by email
            var adminByEmail = await ValidateUserAsync("admin@miningops.com", "admin123");
            Debug.WriteLine($"🧪 Admin by email test: {adminByEmail}");

            // Test wrong password
            var wrongPassResult = await ValidateUserAsync("admin", "wrongpassword");
            Debug.WriteLine($"🧪 Wrong password test: {wrongPassResult}");

            // Test non-existent user
            var nonExistentResult = await ValidateUserAsync("nonexistent", "password");
            Debug.WriteLine($"🧪 Non-existent user test: {nonExistentResult}");
        }
    }
}