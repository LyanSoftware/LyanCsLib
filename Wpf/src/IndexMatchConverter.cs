using System;
using System.Globalization;
using System.Windows.Data;

namespace Lytec.Wpf;

public class IndexMatchConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index && parameter is string s && int.TryParse(s, out int target))
            return index == target;
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
