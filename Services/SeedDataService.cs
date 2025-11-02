using Microsoft.Extensions.Configuration;
using MiningOps.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using MiningOps.Security;

namespace MiningOps.Services
{
    public class SeedDataService
    {
        private readonly IConfiguration _configuration;
        private readonly UserService _userService;
        private readonly DatabaseService _databaseService;

        public SeedDataService(IConfiguration configuration, UserService userService, DatabaseService databaseService)
        {
            _configuration = configuration;
            _userService = userService;
            _databaseService = databaseService;
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                Debug.WriteLine("🌱 Starting database seeding...");

                // Create demo users
                await CreateDemoUsersAsync();

                // Test authentication
                await _userService.TestAuthenticationAsync();

                Debug.WriteLine("✅ Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 Database seeding failed: {ex.Message}");
                Debug.WriteLine($"💥 Stack trace: {ex.StackTrace}");
                // Don't throw, just log - we want the app to start even if seeding fails
            }
        }

        private async Task CreateDemoUsersAsync()
        {
            var demoUsers = new List<(string username, string password, string fullName, string email, string phone, UserRole role)>
            {
                ("admin", "admin123", "System Administrator", "admin@miningops.com", "555-0001", UserRole.Admin),
                ("supplier1", "supplier123", "Supplier One", "supplier1@miningops.com", "555-0002", UserRole.Supplier),
                ("supervisor1", "supervisor123", "Supervisor One", "supervisor1@miningops.com", "555-0003", UserRole.Supervisor)
            };

            foreach (var (username, password, fullName, email, phone, role) in demoUsers)
            {
                try
                {
                    var existingUser = await _userService.GetUserByUsernameAsync(username);
                    if (existingUser == null)
                    {
                        var newUser = new RegisterMining
                        {
                            Username = username,
                            FullName = fullName,
                            Email = email,
                            PhoneNumber = phone,
                            Salt = PasswordHasher.GenerateSalt(), // ✅ UPDATED: Generate proper salt
                            Role = role,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = null
                        };

                        // Password will be hashed in CreateUserAsync using SHA256
                        await _userService.CreateUserAsync(newUser, password);
                        Debug.WriteLine($"✅ Created user: {username}");
                    }
                    else
                    {
                        Debug.WriteLine($"ℹ️ User already exists: {username}");

                        // ✅ OPTIONAL: Update existing demo users to use SHA256 hashing
                        // Uncomment if you want to automatically migrate existing demo users
                        // await _userService.UpdateUserPasswordAsync(existingUser.AccId, password);
                        // Debug.WriteLine($"✅ Updated password hashing for: {username}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"⚠️ Error creating user {username}: {ex.Message}");
                }
            }
        }
    }
}