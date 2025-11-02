using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace MiningOps.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value?.ToString()?.ToLowerInvariant();
            return status switch
            {
                "active" => Brushes.Green,
                "inactive" => Brushes.Red,
                "pending" => Brushes.Orange,
                "approved" => Brushes.Green,
                "rejected" => Brushes.Red,
                "fulfilled" => Brushes.Blue,
                "available" => Brushes.Green,
                "low stock" => Brushes.Orange,
                "out of stock" => Brushes.Red,
                "ordered" => Brushes.Blue,
                "shipped" => Brushes.Teal,
                "delivered" => Brushes.Green,
                "cancelled" => Brushes.Red,
                "completed" => Brushes.Green,
                "unpaid" => Brushes.Red,
                "paid" => Brushes.Green,
                "overdue" => Brushes.Orange,
                "partiallypaid" => Brushes.Yellow,
                _ => Brushes.Gray,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    public class ReadNotificationToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRead)
            {
                return isRead ? Brushes.White : new SolidColorBrush(Color.FromArgb(20, 25, 118, 210));
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}


