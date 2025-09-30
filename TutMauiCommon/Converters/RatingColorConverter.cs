using System.Globalization;
namespace TutMauiCommon.Converters;

public class RatingColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double rating && parameter is string param && int.TryParse(param, out int starPosition))
        {
            return rating >= starPosition ? Color.FromArgb("#FFD700") : Color.FromArgb("#E5E7EB");
        }
        return Colors.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class RatingVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int rating && parameter is string param && int.TryParse(param, out int starPosition))
        {
            return rating >= starPosition;
        }
        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

