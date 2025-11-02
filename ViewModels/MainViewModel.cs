using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiningOps.Models.Entities;
using MiningOps.Services;
using MiningOps.Utilities;
using MiningOps.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Effects;

namespace MiningOps.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IAuthenticationService _authService;
        private readonly IConfiguration _configuration;
        private readonly UserService _userService;
        private object _currentView;
        private bool _isLoggedIn;
        private string _welcomeMessage = "Welcome";
        private readonly INotificationService _notificationService;
        private ObservableCollection<Notification> _notifications;
        private bool _isNotificationsOpen;
        private bool _isUserProfileOpen;
        private int _unreadNotificationCount;
        private readonly IServiceProvider _serviceProvider;

        public MainViewModel(IAuthenticationService authService, IConfiguration configuration,
                           INotificationService notificationService, UserService userService, IServiceProvider serviceProvider)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService)); // ADDED: UserService for supplier profile lookup
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            Debug.WriteLine("🏗️ MainViewModel initialized with dependencies");

            // Initialize commands - FIXED: Remove duplicate ViewAllNotificationsCommand
            NavigateCommand = new RelayCommand<string>(Navigate);
            LogoutCommand = new RelayCommand(Logout);
            ToggleNotificationsCommand = new RelayCommand(ToggleNotifications);
            ToggleUserProfileCommand = new RelayCommand(ToggleUserProfile);
            MarkAllNotificationsReadCommand = new RelayCommand(async () => await MarkAllNotificationsReadAsync());
            ViewAllNotificationsCommand = new RelayCommand(ViewAllNotifications); // KEEP THIS ONE
            NotificationClickCommand = new RelayCommand<Notification>(OnNotificationClick);
            EditProfileCommand = new RelayCommand(EditProfile);
            PreferencesCommand = new RelayCommand(ShowPreferences);
            // REMOVED: ViewAllNotificationsCommand = new RelayCommand(() => NavigateToNotificationsPage()); // DUPLICATE

            // Initialize collections
            NavigationItems = new ObservableCollection<NavigationItem>();
            RoleSpecificItems = new ObservableCollection<NavigationItem>();
            Notifications = new ObservableCollection<Notification>();

            // Start with login view
            ShowLoginView();

            // Subscribe to authentication changes
            _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
        }

        public object CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public bool IsLoggedIn
        {
            get => _isLoggedIn;
            set => SetProperty(ref _isLoggedIn, value);
        }

        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }
        public ObservableCollection<Notification> Notifications
        {
            get => _notifications;
            set => SetProperty(ref _notifications, value);
        }

        public bool IsNotificationsOpen
        {
            get => _isNotificationsOpen;
            set => SetProperty(ref _isNotificationsOpen, value);
        }

        public bool IsUserProfileOpen
        {
            get => _isUserProfileOpen;
            set => SetProperty(ref _isUserProfileOpen, value);
        }

        public int UnreadNotificationCount
        {
            get => _unreadNotificationCount;
            set => SetProperty(ref _unreadNotificationCount, value);
        }

        public bool HasUnreadNotifications => UnreadNotificationCount > 0;
        public bool HasNotifications => Notifications?.Any() == true;
        public string UserProfileName => _authService.CurrentUser?.FullName ?? "User";

        public string UserRoleDisplay => _authService.CurrentUser?.Role.ToString() ?? "Guest";

        // Navigation properties
        public ObservableCollection<NavigationItem> NavigationItems { get; }
        public ObservableCollection<NavigationItem> RoleSpecificItems { get; }

        // Role-based visibility properties
        public bool IsAdmin => _authService.IsInRole(UserRole.Admin);
        public bool IsSupervisor => _authService.IsInRole(UserRole.Supervisor);
        public bool IsSupplier => _authService.IsInRole(UserRole.Supplier);
        public bool IsAdminOrSupervisor => IsAdmin || IsSupervisor;

        public ICommand NavigateCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand ToggleNotificationsCommand { get; }
        public ICommand ToggleUserProfileCommand { get; }
        public ICommand MarkAllNotificationsReadCommand { get; }
        public ICommand ViewAllNotificationsCommand { get; }
        public ICommand NotificationClickCommand { get; }
        public ICommand EditProfileCommand { get; }
        public ICommand PreferencesCommand { get; }

        private void ShowLoginView()
        {
            Debug.WriteLine("🔐 Showing login view");

            var loginViewModel = new LoginViewModel(_authService);
            loginViewModel.OnLoginSuccess += OnLoginSuccessful;
            loginViewModel.OnLoginFailed += OnLoginFailed;
            CurrentView = loginViewModel;
            IsLoggedIn = false;
            WelcomeMessage = "Welcome to MiningOps";
        }

        private void OnLoginSuccessful()
        {
            Debug.WriteLine($"✅ Login successful - User: {_authService.CurrentUser?.Username}, Role: {_authService.CurrentUser?.Role}");

            IsLoggedIn = true;
            UpdateUserInfo();
            InitializeNavigation();
            NavigateToDefaultView();
        }

        private void OnLoginFailed(string errorMessage)
        {
            Debug.WriteLine($"❌ Login failed: {errorMessage}");
            // Error message is already displayed in the LoginViewModel
        }

        private void OnAuthenticationStateChanged(object? sender, System.EventArgs e)
        {
            Debug.WriteLine($"🔄 Authentication state changed - Authenticated: {_authService.IsAuthenticated}");

            if (!_authService.IsAuthenticated)
            {
                ShowLoginView();
            }
        }

        private void ViewAllNotifications()
        {
            NavigateToNotificationsPage();
        }

        private void NavigateToNotificationsPage()
        {
            Navigate("Notifications");
            IsNotificationsOpen = false; // Close the dropdown
        }

        private void UpdateUserInfo()
        {
            var user = _authService.CurrentUser;
            if (user != null)
            {
                WelcomeMessage = $"Welcome, {user.FullName}";
                OnPropertyChanged(nameof(UserRoleDisplay));
                OnPropertyChanged(nameof(IsAdmin));
                OnPropertyChanged(nameof(IsSupervisor));
                OnPropertyChanged(nameof(IsSupplier));
                OnPropertyChanged(nameof(IsAdminOrSupervisor));

                Debug.WriteLine($"👤 User info updated - {user.FullName} ({user.Role})");
            }
            else
            {
                Debug.WriteLine("⚠️ MainViewModel: No current user found during UpdateUserInfo");
            }
        }

        private async void ToggleNotifications()
        {
            IsNotificationsOpen = !IsNotificationsOpen;
            if (IsNotificationsOpen)
            {
                await LoadNotificationsAsync();
            }
        }

        private void ToggleUserProfile()
        {
            IsUserProfileOpen = !IsUserProfileOpen;
        }

        private async Task LoadNotificationsAsync()
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser != null)
                {
                    var notifications = await _notificationService.GetNotificationsForUserAsync(currentUser.Role, currentUser.AccId);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Notifications.Clear();
                        foreach (var notification in notifications)
                        {
                            Notifications.Add(notification);
                        }
                    });

                    // Update unread count
                    UnreadNotificationCount = await _notificationService.GetUnreadCountAsync(currentUser.Role, currentUser.AccId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 Error loading notifications: {ex.Message}");
            }
        }

        private async Task MarkAllNotificationsReadAsync()
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser != null)
                {
                    await _notificationService.MarkAllAsReadAsync(currentUser.AccId);
                    await LoadNotificationsAsync(); // Refresh the list
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 Error marking notifications as read: {ex.Message}");
            }
        }

        private async void OnNotificationClick(Notification notification)
        {
            if (notification != null)
            {
                // Mark as read
                await _notificationService.MarkAsReadAsync(notification.NotificationId);

                // Navigate based on notification type
                switch (notification.RelatedEntityType)
                {
                    case "Order":
                        Navigate("Orders");
                        break;
                    case "Request":
                        Navigate("Requests");
                        break;
                    case "Payment":
                        Navigate("Payments");
                        break;
                    case "Invoice":
                        Navigate("Payments"); // Assuming invoices are in payments section
                        break;
                }

                IsNotificationsOpen = false;
                await LoadNotificationsAsync(); // Refresh count
            }
        }

        private void EditProfile()
        {
            try
            {
                var dialog = new UserProfileDialog();
                var userProfileViewModel = _serviceProvider.GetService<UserProfileViewModel>();
                dialog.DataContext = userProfileViewModel;

                if (dialog.ShowDialog() == true)
                {
                    // Profile was updated
                    UpdateUserInfo();
                    MessageBox.Show("Profile updated successfully", "Success",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }

                IsUserProfileOpen = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error editing profile: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowPreferences()
        {
            MessageBox.Show("Preferences dialog would open here", "Preferences",
                           MessageBoxButton.OK, MessageBoxImage.Information);
            IsUserProfileOpen = false;
        }

        private void InitializeNavigation()
        {
            NavigationItems.Clear();
            RoleSpecificItems.Clear();

            // Common navigation items for all roles
            //NavigationItems.Add(new NavigationItem("Dashboard", "📊 Dashboard", "Dashboard", true));

            // Role-specific navigation
            if (IsAdmin)
            {
                NavigationItems.Add(new NavigationItem("Dashboard", "📊 Dashboard", "Dashboard", true));
                InitializeAdminNavigation();
                Debug.WriteLine("🧭 MainViewModel: Admin navigation initialized");
            }
            else if (IsSupervisor)
            {
                InitializeSupervisorNavigation();
                Debug.WriteLine("🧭 MainViewModel: Supervisor navigation initialized");
            }
            else if (IsSupervisor)
            {
                NavigationItems.Add(new NavigationItem("Dashboard", "📊 Supervisor Dashboard", "Dashboard", true));
                InitializeSupervisorNavigation();
                Debug.WriteLine("🧭 MainViewModel: Supervisor navigation initialized");
            }
            else if (IsSupplier)
            {
                NavigationItems.Add(new NavigationItem("Dashboard", "📊 Supplier Dashboard", "Dashboard", true));
                InitializeSupplierNavigation();
                Debug.WriteLine("🧭 MainViewModel: Supplier navigation initialized");
            }
            else
            {
                Debug.WriteLine("⚠️ MainViewModel: No role detected for navigation");
            }

            Debug.WriteLine($"🧭 MainViewModel: Navigation initialized for {UserRoleDisplay}");
        }

        private void InitializeAdminNavigation()
        {
            NavigationItems.Add(new NavigationItem("UserManagement", "👥 User Management", "UserManagement", true));
            NavigationItems.Add(new NavigationItem("Inventory", "📦 Inventory", "Inventory", true));
            NavigationItems.Add(new NavigationItem("Suppliers", "🏢 Suppliers", "Suppliers", true));
            NavigationItems.Add(new NavigationItem("Requests", "📋 Requests", "Requests", true));
            NavigationItems.Add(new NavigationItem("Orders", "🛒 Orders & Procurement", "Orders", true));
            NavigationItems.Add(new NavigationItem("Payments", "💳 Payment Processing", "Payments", true));

            RoleSpecificItems.Add(new NavigationItem("AdminPanel", "⚙️ Admin Panel", "Dashboard", false));
        }

        private void InitializeSupervisorNavigation()
        {
            NavigationItems.Add(new NavigationItem("Inventory", "📦 Inventory", "Inventory", true));
            NavigationItems.Add(new NavigationItem("Requests", "📋 Requests", "Requests", true));
            NavigationItems.Add(new NavigationItem("Orders", "🛒 Orders & Procurement", "Orders", true));

            RoleSpecificItems.Add(new NavigationItem("Supervisor", "👨‍💼 Supervisor View", "Dashboard", false));
        }

        private void InitializeSupplierNavigation()
        {
            NavigationItems.Add(new NavigationItem("SupplierPortal", "🏠 Supplier Portal", "SupplierPortal", true));
            NavigationItems.Add(new NavigationItem("Orders", "📦 My Orders", "Orders", true));

            RoleSpecificItems.Add(new NavigationItem("Supplier", "🤝 Supplier Portal", "SupplierPortal", false));
        }

        private void NavigateToDefaultView()
        {
            Debug.WriteLine($"🧭 MainViewModel: Navigating to default view for {UserRoleDisplay}");

            if (IsSupplier)
            {
                Navigate("SupplierPortal");
            }
            else
            {
                Navigate("Dashboard");
            }
        }

        // CHANGED: Made this method async
        private async void Navigate(string destination)
        {
            if (!IsLoggedIn)
            {
                Debug.WriteLine("⚠️ MainViewModel: Cannot navigate - user not logged in");
                return;
            }

            // Check if user has access to this view
            if (!HasAccessToView(destination))
            {
                MessageBox.Show("You don't have permission to access this section.",
                    "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Debug.WriteLine($"🧭 MainViewModel: Navigating to: {destination}");

            try
            {
                switch (destination)
                {
                    case "Dashboard":
                        CurrentView = _serviceProvider.GetService<DashboardViewModel>();
                        break;
                    case "UserManagement":
                        if (IsAdmin) CurrentView = _serviceProvider.GetService<UserManagementViewModel>();
                        break;
                    case "Inventory":
                        CurrentView = _serviceProvider.GetService<InventoryManagementViewModel>();
                        break;
                    case "Suppliers":
                        if (IsAdmin) CurrentView = _serviceProvider.GetService<SupplierManagementViewModel>();
                        break;
                    case "Requests":
                        CurrentView = _serviceProvider.GetService<RequestManagementViewModel>();
                        break;
                    case "Orders":
                        CurrentView = _serviceProvider.GetService<OrderManagementViewModel>();
                        break;
                    case "Payments":
                        if (IsAdmin) CurrentView = _serviceProvider.GetService<PaymentProcessingViewModel>();
                        break;
                    case "SupplierPortal":
                        if (IsSupplier && _authService.CurrentUser != null)
                        {
                            // Get the supplier profile for the current user
                            var supplierProfile = await _userService.GetSupplierProfileAsync(_authService.CurrentUser.AccId);
                            if (supplierProfile != null)
                            {
                                CurrentView = _serviceProvider.GetService<SupplierDashboardViewModel>();
                                Debug.WriteLine($"✅ Navigated to Supplier Portal for {supplierProfile.CompanyName}");
                            }
                            else
                            {
                                Debug.WriteLine($"❌ No supplier profile found for user {_authService.CurrentUser.Username}");
                                MessageBox.Show("No supplier profile found for your account. Please contact administrator.",
                                    "Supplier Profile Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                                CurrentView = _serviceProvider.GetService<DashboardViewModel>();
                            }
                        }
                        break;
                    case "Notifications":
                        CurrentView = _serviceProvider.GetService<NotificationsViewModel>();
                        break;
                    default:
                        CurrentView = _serviceProvider.GetService<DashboardViewModel>();
                        break;
                }

                Debug.WriteLine($"✅ MainViewModel: Successfully navigated to {destination}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"💥 MainViewModel: Navigation error to {destination}: {ex.Message}");
                MessageBox.Show($"Error loading {destination}: {ex.Message}", "Navigation Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // Add this method to MainViewModel
        public void NavigateToDashboard()
        {
            Navigate("Dashboard");
        }
        private bool HasAccessToView(string view)
        {
            return view switch
            {
                "UserManagement" or "Suppliers" or "Payments" => IsAdmin,
                "SupplierPortal" => IsSupplier,
                _ => true // Dashboard, Inventory, Requests, Orders are accessible to multiple roles
            };
        }

        private void Logout()
        {
            Debug.WriteLine("🚪 MainViewModel: User initiated logout");
            _authService.Logout();
        }

        // Cleanup method
        public void Cleanup()
        {
            _authService.AuthenticationStateChanged -= OnAuthenticationStateChanged;
            Debug.WriteLine("🧹 MainViewModel cleanup completed");
        }


        public class NavigationItem
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
            public string CommandParameter { get; set; }
            public bool IsNavigation { get; set; }

            public NavigationItem(string id, string displayName, string commandParameter, bool isNavigation)
            {
                Id = id;
                DisplayName = displayName;
                CommandParameter = commandParameter;
                IsNavigation = isNavigation;
            }
        }
    }
}