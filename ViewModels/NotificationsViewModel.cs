using MiningOps.Models.Entities;
using MiningOps.Services;
using MiningOps.Utilities;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls; // Add this for MainWindow
using System.Linq; // Add this for OfType

namespace MiningOps.ViewModels
{
    public class NotificationsViewModel : BaseViewModel
    {
        private readonly INotificationService _notificationService;
        private readonly IAuthenticationService _authService;
        private ObservableCollection<Notification> _allNotifications;
        private string _filterStatus = "All";
        private string _filterPriority = "All";
        private bool _isLoading;
        private int _totalNotifications;
        private int _unreadCount;
        private int _readCount;

        public NotificationsViewModel(IAuthenticationService authService, INotificationService notificationService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            AllNotifications = new ObservableCollection<Notification>();

            // Initialize commands
            RefreshCommand = new RelayCommand(async () => await LoadAllNotificationsAsync());
            MarkAsReadCommand = new RelayCommand<Notification>(async (notification) => await MarkAsReadAsync(notification));
            MarkAllAsReadCommand = new RelayCommand(async () => await MarkAllAsReadAsync());
            DeleteNotificationCommand = new RelayCommand<Notification>(async (notification) => await DeleteNotificationAsync(notification));
            ClearAllCommand = new RelayCommand(async () => await ClearAllNotificationsAsync());
            FilterCommand = new RelayCommand(async () => await ApplyFiltersAsync());
            BackCommand = new RelayCommand(() => NavigateBack());

            // Load notifications
            _ = LoadAllNotificationsAsync();
        }

        public ObservableCollection<Notification> AllNotifications
        {
            get => _allNotifications;
            set => SetProperty(ref _allNotifications, value);
        }

        public string FilterStatus
        {
            get => _filterStatus;
            set => SetProperty(ref _filterStatus, value);
        }

        public string FilterPriority
        {
            get => _filterPriority;
            set => SetProperty(ref _filterPriority, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public int TotalNotifications
        {
            get => _totalNotifications;
            set => SetProperty(ref _totalNotifications, value);
        }

        public int UnreadCount
        {
            get => _unreadCount;
            set => SetProperty(ref _unreadCount, value);
        }

        public int ReadCount
        {
            get => _readCount;
            set => SetProperty(ref _readCount, value);
        }

        // Computed properties
        public string PageTitle => $"Notifications ({TotalNotifications})";
        public bool HasNotifications => AllNotifications.Any();
        public bool HasUnreadNotifications => UnreadCount > 0;

        // Commands - ADD THE BACKCOMMAND PROPERTY HERE
        public ICommand RefreshCommand { get; }
        public ICommand MarkAsReadCommand { get; }
        public ICommand MarkAllAsReadCommand { get; }
        public ICommand DeleteNotificationCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand BackCommand { get; } // ADD THIS LINE

        private async Task LoadAllNotificationsAsync()
        {
            IsLoading = true;

            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser != null)
                {
                    var notifications = await _notificationService.GetAllNotificationsForUserAsync(currentUser.Role, currentUser.AccId);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AllNotifications.Clear();
                        foreach (var notification in notifications)
                        {
                            AllNotifications.Add(notification);
                        }

                        // Update statistics
                        UpdateNotificationStats(notifications);
                    });

                    Debug.WriteLine($"✅ Loaded {notifications.Count} notifications for {currentUser.Role}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"💥 Error loading notifications: {ex.Message}");
                MessageBox.Show($"Error loading notifications: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void NavigateBack()
        {
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (mainWindow?.DataContext is MainViewModel mainViewModel)
            {
                mainViewModel.NavigateToDashboard();
            }
        }

        private void UpdateNotificationStats(System.Collections.Generic.List<Notification> notifications)
        {
            TotalNotifications = notifications.Count;
            UnreadCount = notifications.Count(n => !n.IsRead);
            ReadCount = notifications.Count(n => n.IsRead);

            OnPropertyChanged(nameof(PageTitle));
            OnPropertyChanged(nameof(HasNotifications));
            OnPropertyChanged(nameof(HasUnreadNotifications));
        }

        private async Task ApplyFiltersAsync()
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser != null)
                {
                    var allNotifications = await _notificationService.GetAllNotificationsForUserAsync(currentUser.Role, currentUser.AccId);

                    var filteredNotifications = allNotifications.Where(n =>
                        (FilterStatus == "All" || (FilterStatus == "Unread" && !n.IsRead) || (FilterStatus == "Read" && n.IsRead)) &&
                        (FilterPriority == "All" || n.Priority.ToString() == FilterPriority)
                    ).ToList();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AllNotifications.Clear();
                        foreach (var notification in filteredNotifications)
                        {
                            AllNotifications.Add(notification);
                        }
                        UpdateNotificationStats(filteredNotifications);
                    });

                    Debug.WriteLine($"🔍 Applied filters - Status: {FilterStatus}, Priority: {FilterPriority}, Results: {filteredNotifications.Count}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"❌ Error applying filters: {ex.Message}");
                MessageBox.Show($"Error applying filters: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task MarkAsReadAsync(Notification notification)
        {
            if (notification == null) return;

            try
            {
                await _notificationService.MarkAsReadAsync(notification.NotificationId);
                notification.IsRead = true;

                // Update statistics
                UnreadCount--;
                ReadCount++;
                OnPropertyChanged(nameof(HasUnreadNotifications));

                Debug.WriteLine($"✅ Marked notification {notification.NotificationId} as read");
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"❌ Error marking notification as read: {ex.Message}");
                MessageBox.Show($"Error marking notification as read: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task MarkAllAsReadAsync()
        {
            try
            {
                var currentUser = _authService.CurrentUser;
                if (currentUser != null)
                {
                    await _notificationService.MarkAllAsReadAsync(currentUser.AccId);
                    await LoadAllNotificationsAsync(); // Refresh to update all states

                    MessageBox.Show("All notifications marked as read", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"❌ Error marking all as read: {ex.Message}");
                MessageBox.Show($"Error marking all notifications as read: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DeleteNotificationAsync(Notification notification)
        {
            if (notification == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete this notification?\n\n\"{notification.Title}\"",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    bool success = await _notificationService.DeleteNotificationAsync(notification.NotificationId);
                    if (success)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            AllNotifications.Remove(notification);
                            UpdateNotificationStats(AllNotifications.ToList());
                        });

                        Debug.WriteLine($"🗑️ Deleted notification {notification.NotificationId}");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.WriteLine($"❌ Error deleting notification: {ex.Message}");
                    MessageBox.Show($"Error deleting notification: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ClearAllNotificationsAsync()
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear ALL notifications? This action cannot be undone.",
                "Confirm Clear All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var currentUser = _authService.CurrentUser;
                    if (currentUser != null)
                    {
                        await _notificationService.ClearAllNotificationsAsync(currentUser.Role, currentUser.AccId);
                        await LoadAllNotificationsAsync(); // Refresh to show empty state

                        MessageBox.Show("All notifications cleared", "Success",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.WriteLine($"❌ Error clearing all notifications: {ex.Message}");
                    MessageBox.Show($"Error clearing all notifications: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}