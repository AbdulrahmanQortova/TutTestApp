using System.Globalization;
using Tut.Common.Models;

namespace Tut.Converters
{
    public class TripStateToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TripState state)
            {
                return state switch
                {
                    TripState.Ended => Colors.Green,
                    TripState.Requested => Colors.Yellow,
                    _ => Colors.Red
                };
            }
            return Colors.Red;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
