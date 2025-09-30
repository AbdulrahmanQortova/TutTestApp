using System.Globalization;
using Tut.Common.Models;

namespace TutMauiCommon.Converters;

public class StatusConverters : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DriverState status)
        {
            return status switch
            {
                DriverState.Available => Color.FromArgb("#F3FAF3"),
                DriverState.Inactive => Color.FromArgb("#FFF5F6"),  
                DriverState.Unspecified => Color.FromArgb("#FFFAE0"),  
                DriverState.OnTrip => Color.FromArgb("#F0F8FF"), 
                _ => Colors.Transparent
            };
        }
        return Colors.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StatusTextColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DriverState status)
        {
            return status switch
            {
                DriverState.Available   => Color.FromArgb("#4CAF50"),
                DriverState.Inactive    => Color.FromArgb("#F44336"),
                DriverState.Unspecified => Color.FromArgb("#B8860B"),
                DriverState.OnTrip      => Color.FromArgb("#1976D2"),
                DriverState.Offline     => Color.FromArgb("#FF0000"),
                _ => Colors.Gray
            };
        }
        return Colors.Black;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
