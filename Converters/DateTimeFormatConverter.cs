using System.Globalization;

namespace Ticket.Converters
{
    public class DateTimeFormatConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
                return dt.ToString("MMM dd, yyyy HH:mm");
            return "-";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
