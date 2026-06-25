using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Newtonsoft.Json.Linq;

namespace Lytec.Wpf;

public class TimeSpanTotalSecondsConverter : IValueConverter
{
    static object Convert(object value)
    {
        switch (value)
        {
            case int seconds: return TimeSpan.FromSeconds(seconds);
            case uint seconds: return TimeSpan.FromSeconds(seconds);
            case TimeSpan ts: return (int)ts.TotalSeconds;
            default: throw new NotSupportedException();
        }
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    => Convert(value);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    => Convert(value);
}
