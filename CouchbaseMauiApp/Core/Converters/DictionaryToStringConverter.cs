using System.Globalization;
using System.Text.Json;

namespace CouchbaseMauiApp.Core.Converters;

public class DictionaryToStringConverter : IValueConverter
{
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        if (value is Dictionary<string, object> dict)
        {
            return JsonSerializer.Serialize(dict);
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        if (value is string str)
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(str);
        }
        return new Dictionary<string, object>();
    }
} 