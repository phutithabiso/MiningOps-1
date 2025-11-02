using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiningOps.Dialogs;
using MiningOps.Services;
using MiningOps.ViewModels;
using MiningOps.ViewModels.Dialogs;
using MiningOps.Views;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace MiningOps
{
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider;
        public IServiceProvider ServiceProvider => _serviceProvider;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Test database connection and initialize
            await InitializeDatabaseAsync();

            var mainWindow = _serviceProvider.GetService<MainWindow>();
            mainWindow?.Show();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // Configuration
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            services.AddSingleton<IConfiguration>(configuration);
            // Dialog registrations
        services.AddTransient<SupplierDialog>(provider =>
        new SupplierDialog(provider.GetRequiredService<UserService>()));
            services.AddTransient<RequestDialog>();
            services.AddTransient<InventoryItemDialog>();
            services.AddTransient<UserDialog>();
            services.AddTransient<UserProfileDialog>();
            // Core Services
            services.AddSingleton<DatabaseService>();
            services.AddSingleton<SeedDataService>();

            // Business Services - Register in dependency order
            services.AddSingleton<UserService>();
            services.AddSingleton<IAuthenticationService, AuthenticationService>();
            services.AddSingleton<SupplierService>();
            services.AddSingleton<InventoryService>();
            services.AddSingleton<WarehouseService>();
            services.AddSingleton<RequestService>();
            services.AddSingleton<OrderService>();
            services.AddSingleton<InvoiceService>();
            services.AddTransient<UserDetailsDialog>();
            services.AddSingleton<PaymentService>();
            services.AddSingleton<INotificationService, NotificationService>();

            // ViewModels - ALL registered with proper dependencies
            services.AddTransient<MainViewModel>();
            services.AddTransient<LoginViewModel>();

            // DashboardViewModel with all required services
            // DashboardViewModel with authentication service
            services.AddTransient<DashboardViewModel>(provider =>
                new DashboardViewModel(
                    provider.GetRequiredService<IAuthenticationService>(), // Add this
                    provider.GetRequiredService<UserService>(),
                    provider.GetRequiredService<InventoryService>(),
                    provider.GetRequiredService<SupplierService>(),
                    provider.GetRequiredService<WarehouseService>(),
                    provider.GetRequiredService<RequestService>(),
                    provider.GetRequiredService<OrderService>(),
                    provider.GetRequiredService<PaymentService>(),
                    provider.GetRequiredService<InvoiceService>()));

            // UserManagementViewModel
            services.AddTransient<UserManagementViewModel>(provider =>
                new UserManagementViewModel(
                    provider.GetRequiredService<UserService>()));

            // InventoryManagementViewModel
            services.AddTransient<InventoryManagementViewModel>(provider =>
                new InventoryManagementViewModel(
                    provider.GetRequiredService<InventoryService>(),
                    provider.GetRequiredService<WarehouseService>()));

            // SupplierManagementViewModel
            services.AddTransient<SupplierManagementViewModel>(provider =>
                new SupplierManagementViewModel(
                    provider.GetRequiredService<SupplierService>(),
                    provider.GetRequiredService<UserService>(),
                    provider.GetRequiredService<OrderService>(),
                    provider.GetRequiredService<IServiceProvider>()));

            // RequestManagementViewModel
            services.AddTransient<RequestManagementViewModel>(provider =>
                new RequestManagementViewModel(
                    provider.GetRequiredService<RequestService>()));

            // OrderManagementViewModel
            services.AddTransient<OrderManagementViewModel>(provider =>
             new OrderManagementViewModel(
                 provider.GetRequiredService<OrderService>(),
                 provider.GetRequiredService<SupplierService>(),
                 provider.GetRequiredService<RequestService>(),
                 provider.GetRequiredService<IAuthenticationService>() // Add this
             ));

            // PaymentProcessingViewModel
            services.AddTransient<PaymentProcessingViewModel>(provider =>
                new PaymentProcessingViewModel(
                    provider.GetRequiredService<PaymentService>(),
                    provider.GetRequiredService<InvoiceService>(),
                    provider.GetRequiredService<OrderService>()));

            // SupplierDashboardViewModel
            services.AddTransient<SupplierDashboardViewModel>(provider =>
                new SupplierDashboardViewModel(
                    provider.GetRequiredService<IAuthenticationService>(),
                    provider.GetRequiredService<IConfiguration>(),
                    provider.GetRequiredService<UserService>()));

            // NotificationsViewModel
            services.AddTransient<NotificationsViewModel>(provider =>
                new NotificationsViewModel(
                    provider.GetRequiredService<IAuthenticationService>(),
                    provider.GetRequiredService<INotificationService>()));

            // UserProfileViewModel
            services.AddTransient<UserProfileViewModel>(provider =>
                new UserProfileViewModel(
                    provider.GetRequiredService<IAuthenticationService>(),
                    provider.GetRequiredService<UserService>()));

            // Dialog ViewModels
            services.AddTransient<SupplierDialogViewModel>(provider =>
                new SupplierDialogViewModel(
                    provider.GetRequiredService<UserService>()));

            services.AddTransient<InventoryItemDialogViewModel>();

            // Views and Dialogs
            services.AddTransient<UserProfileDialog>();
            services.AddTransient<SupplierDialog>();
            services.AddTransient<DashboardView>();
            services.AddTransient<SupplierManagementView>();

            // Service Provider for navigation
            services.AddSingleton<IServiceProvider>(provider => provider);

            // Main Window
            services.AddSingleton<MainWindow>(provider => new MainWindow
            {
                DataContext = provider.GetRequiredService<MainViewModel>()
            });
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                var dbService = _serviceProvider.GetRequiredService<DatabaseService>();
                var userService = _serviceProvider.GetRequiredService<UserService>();
                var seedService = _serviceProvider.GetRequiredService<SeedDataService>();

                Debug.WriteLine("🔧 Initializing database...");

                var connectionSuccess = await dbService.TestConnectionAsync();
                Debug.WriteLine($"🔧 Database connection: {connectionSuccess}");

                if (connectionSuccess)
                { // Try to recover existing admin first
                    var adminRecovered = await userService.RecoverAdminUserAsync();
                    if (!adminRecovered)
                    {
                        // Create default admin user first
                        var adminCreated = await userService.CreateDefaultAdminUserAsync();
                        Debug.WriteLine($"🔧 Default admin creation: {(adminCreated ? "Success" : "Failed or already exists")}");
                    }
                    // Wait for seed data to complete so users are available for login
                    await seedService.InitializeDatabaseAsync();
                    Debug.WriteLine("✅ Database seeded successfully");
                }
                else
                {
                    Debug.WriteLine("⚠️ Database connection failed, but continuing with in-memory data");
                    // Even if DB fails, we'll use in-memory demo users
                    await seedService.InitializeDatabaseAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Database initialization failed: {ex.Message}");
                // Continue with the app even if seeding fails
                MessageBox.Show("Application started with demo data. Some features may be limited.",
                    "Initialization Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }
    }
}