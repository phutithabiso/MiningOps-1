using System.Collections.Generic;

namespace MiningOps.Views
{
    public static class NotificationViewFilters
    {
        public static List<string> StatusFilters => new List<string>
        {
            "All",
            "Unread",
            "Read"
        };

        public static List<string> PriorityFilters => new List<string>
        {
            "All",
            "Critical",
            "High",
            "Medium",
            "Low"
        };
    }
}