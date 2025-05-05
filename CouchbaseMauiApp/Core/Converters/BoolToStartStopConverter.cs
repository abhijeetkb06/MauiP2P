using System.Globalization;

namespace CouchbaseMauiApp.Core.Converters;

public class BoolToStartStopConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        if (value is bool isReceiving)
        {
            return isReceiving ? "Stop" : "Start Receive";
        }
        return "Start Receive";
    }

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        if (value is string str)
        {
            return str == "Stop";
        }
        return false;
    }
} 