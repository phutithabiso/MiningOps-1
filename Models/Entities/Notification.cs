using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;

namespace MiningOps.Models.Entities
{
    public class Notification
    {
        public int NotificationId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public NotificationPriority Priority { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? RelatedEntityId { get; set; } // OrderId, RequestId, etc.
        public string RelatedEntityType { get; set; } // "Order", "Request", "Invoice", "Payment"
        public UserRole TargetRole { get; set; } // Which role should see this notification
    }

    public enum NotificationType
    {
        NewOrder,
        OrderStatusChange,
        NewRequest,
        RequestStatusChange,
        NewInvoice,
        PaymentReceived,
        LowInventory,
        SystemAlert
    }

    public enum NotificationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}