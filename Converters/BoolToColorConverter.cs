using System.Globalization;

namespace Ticket.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public Color? TrueValue { get; set; }
        public Color? FalseValue { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (TrueValue is not null && FalseValue is not null)
                return value is true ? TrueValue : FalseValue;

            return value is bool b
                ? Color.FromArgb(b ? "#10B981" : "#F59E0B")
                : Color.FromArgb("#6B7280");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
