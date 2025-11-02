using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using MiningOps.Models.Entities;

namespace MiningOps.Services
{
    public interface INotificationService
    {
        Task<List<Notification>> GetNotificationsForUserAsync(UserRole userRole, int userId);
        Task<List<Notification>> GetAllNotificationsForUserAsync(UserRole userRole, int userId);
        Task AddNotificationAsync(Notification notification);
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync(int userId);
        Task<int> GetUnreadCountAsync(UserRole userRole, int userId);
        Task<bool> DeleteNotificationAsync(int notificationId);
        Task ClearAllNotificationsAsync(UserRole userRole, int userId);
    }

    public class NotificationService : INotificationService
    {
        private readonly List<Notification> _notifications;
        private int _nextId = 1;

        public NotificationService()
        {
            _notifications = new List<Notification>();
            InitializeSampleNotifications();
        }

        private void InitializeSampleNotifications()
        {
            // Sample notifications for different roles
            _notifications.AddRange(new[]
            {
                // Admin notifications
                new Notification
                {
                    NotificationId = _nextId++,
                    Title = "New Material Request",
                    Message = "Supervisor John submitted a new material request for mining equipment",
                    Type = NotificationType.NewRequest,
                    Priority = NotificationPriority.Medium,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-1),
                    RelatedEntityId = 101,
                    RelatedEntityType = "Request",
                    TargetRole = UserRole.Admin
                },
                new Notification
                {
                    NotificationId = _nextId++,
                    Title = "Payment Received",
                    Message = "Payment of $5,000 received from Municipal Corp for invoice #INV-2024-001",
                    Type = NotificationType.PaymentReceived,
                    Priority = NotificationPriority.Medium,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-2),
                    RelatedEntityId = 201,
                    RelatedEntityType = "Payment",
                    TargetRole = UserRole.Admin
                },
                new Notification
                {
                    NotificationId = _nextId++,
                    Title = "System Maintenance",
                    Message = "Scheduled system maintenance this weekend from 10 PM to 2 AM",
                    Type = NotificationType.SystemAlert,
                    Priority = NotificationPriority.Low,
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    RelatedEntityId = null,
                    RelatedEntityType = "System",
                    TargetRole = UserRole.Admin
                },

                // Supervisor notifications
                new Notification
                {
                    NotificationId = _nextId++,
                    Title = "Order Status Updated",
                    Message = "Order #ORD-2024-015 has been dispatched by Mining Supplies Inc.",
                    Type = NotificationType.OrderStatusChange,
                    Priority = NotificationPriority.Medium,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-3),
                    RelatedEntityId = 301,
                    RelatedEntityType = "Order",
                    TargetRole = UserRole.Supervisor
                },
                new Notification
                {
                    NotificationId = _nextId++,
                    Title = "Low Inventory Alert",
                    Message = "Safety helmets are running low (only 15 remaining)",
                    Type = NotificationType.LowInventory,
                    Priority = NotificationPriority.High,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-4),
                    RelatedEntityId = 401,
                    RelatedEntityType = "Inventory",
                    TargetRole = UserRole.Supervisor
                },
                new Notification
                {
                    NotificationId = _nextId++,
                    Title = "Weekly Report Ready",
                    Message = "Weekly inventory report has been generated and is ready for review",
                    Type = NotificationType.SystemAlert,
                    Priority = NotificationPriority.Low,
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    RelatedEntityId = 402,
                    RelatedEntityType = "Report",
                    TargetRole = UserRole.Supervisor
                },

                // Supplier notifications
                new Notification
                {
                    NotificationId = _nextId++,
                    Title = "New Purchase Order",
                    Message = "New order received from Mining Corp for 50 safety helmets",
                    Type = NotificationType.NewOrder,
                    Priority = NotificationPriority.High,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-5),
                    RelatedEntityId = 501,
                    RelatedEntityType = "Order",
                    TargetRole = UserRole.Supplier
                },
                new Notification
                {
                    NotificationId = _nextId++,
                    Title = "Order #ORD-2024-012 Status",
                    Message = "Order has been approved and ready for processing",
                    Type = NotificationType.OrderStatusChange,
                    Priority = NotificationPriority.Medium,
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddHours(-6),
                    RelatedEntityId = 502,
                    RelatedEntityType = "Order",
                    TargetRole = UserRole.Supplier
                },
                new Notification
                {
                    NotificationId = _nextId++,
                    Title = "Payment Processed",
                    Message = "Payment for invoice #INV-2024-008 has been processed",
                    Type = NotificationType.PaymentReceived,
                    Priority = NotificationPriority.Medium,
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    RelatedEntityId = 503,
                    RelatedEntityType = "Payment",
                    TargetRole = UserRole.Supplier
                }
            });
        }

        public async Task<List<Notification>> GetNotificationsForUserAsync(UserRole userRole, int userId)
        {
            await Task.Delay(10); // Simulate async operation
            return _notifications
                .Where(n => n.TargetRole == userRole)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20) // Limit for dropdown
                .ToList();
        }

        // NEW: Get all notifications for user (no limit)
        public async Task<List<Notification>> GetAllNotificationsForUserAsync(UserRole userRole, int userId)
        {
            await Task.Delay(10); // Simulate async operation
            return _notifications
                .Where(n => n.TargetRole == userRole)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }

        public async Task AddNotificationAsync(Notification notification)
        {
            await Task.Delay(10);
            notification.NotificationId = _nextId++;
            notification.CreatedAt = DateTime.UtcNow;
            notification.IsRead = false;
            _notifications.Add(notification);

            Debug.WriteLine($"🔔 New notification: {notification.Title} for {notification.TargetRole}");
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            await Task.Delay(10);
            var notification = _notifications.FirstOrDefault(n => n.NotificationId == notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                Debug.WriteLine($"✅ Marked notification {notificationId} as read");
            }
        }

        public async Task MarkAllAsReadAsync(int userId)
        {
            await Task.Delay(10);
            var userNotifications = _notifications.Where(n => !n.IsRead).ToList();
            foreach (var notification in userNotifications)
            {
                notification.IsRead = true;
            }
            Debug.WriteLine($"✅ Marked all notifications as read");
        }

        public async Task<int> GetUnreadCountAsync(UserRole userRole, int userId)
        {
            await Task.Delay(10);
            return _notifications.Count(n => n.TargetRole == userRole && !n.IsRead);
        }

        // NEW: Delete a specific notification
        public async Task<bool> DeleteNotificationAsync(int notificationId)
        {
            await Task.Delay(10);
            var notification = _notifications.FirstOrDefault(n => n.NotificationId == notificationId);
            if (notification != null)
            {
                _notifications.Remove(notification);
                Debug.WriteLine($"🗑️ Deleted notification {notificationId}");
                return true;
            }
            return false;
        }

        // NEW: Clear all notifications for a user
        public async Task ClearAllNotificationsAsync(UserRole userRole, int userId)
        {
            await Task.Delay(10);
            var userNotifications = _notifications.Where(n => n.TargetRole == userRole).ToList();
            foreach (var notification in userNotifications)
            {
                _notifications.Remove(notification);
            }
            Debug.WriteLine($"🗑️ Cleared all notifications for {userRole}");
        }
    }
}