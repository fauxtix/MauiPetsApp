using MauiPetsApp.Core.Application.Formatting;
using System.Globalization;

namespace MauiPets.Converters;

public class StringToDateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var cultureToUse = culture ?? CultureInfo.CurrentCulture;

        DateTime dateTime;

        if (value is DateTime dt)
        {
            dateTime = dt;
        }
        else if (value is string s && !string.IsNullOrWhiteSpace(s))
        {
            if (!DateTime.TryParse(s, cultureToUse, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out dateTime))
            {
                dateTime = DataFormat.DateParse(s);
            }
        }
        else if (value != null)
        {
            if (!DateTime.TryParse(value.ToString(), cultureToUse, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out dateTime))
            {
                dateTime = DateTime.MinValue;
            }
        }
        else
        {
            return string.Empty;
        }

        if (dateTime == DateTime.MinValue)
            return string.Empty;

        var format = parameter as string;
        if (string.IsNullOrWhiteSpace(format))
        {
            format = cultureToUse.DateTimeFormat.ShortDatePattern;
        }

        return dateTime.ToString(format, cultureToUse);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var cultureToUse = culture ?? CultureInfo.CurrentCulture;

        if (value is DateTime dtValue)
            return dtValue;

        var s = value?.ToString() ?? string.Empty;
        if (DateTime.TryParse(s, cultureToUse, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out var parsed))
        {
            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
                return parsed;

            return parsed.ToString("yyyy-MM-dd");
        }

        return targetType == typeof(string) ? string.Empty : (object)DateTime.MinValue;
    }
}