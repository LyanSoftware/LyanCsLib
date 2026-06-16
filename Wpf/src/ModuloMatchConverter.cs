using System;
using System.Globalization;
using System.Windows.Data;

namespace Lytec.Wpf;

public class ModuloMatchConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index && parameter is string s && int.TryParse(s, out int mod))
            return index % mod == 0;
        return false;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
