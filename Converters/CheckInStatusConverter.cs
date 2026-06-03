using System.Globalization;

namespace Ticket.Converters
{
    public class CheckInStatusConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isCheckedIn)
                return isCheckedIn ? "Checked In" : "Pending";
            return "Unknown";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
