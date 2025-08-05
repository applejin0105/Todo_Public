using System;
using System.Globalization;
using System.Windows.Data;

namespace Todo.Converters
{
    public class UtcToLocalTimeConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not DateTime dateTime)
            {
                return null;
            }

            DateTime localTime;

            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                localTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToLocalTime();
            }
            else
            {
                localTime = dateTime.ToLocalTime();
            }

            return localTime.ToString("yyyy-MM-dd HH:mm");
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}