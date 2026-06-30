using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Lytec.Wpf;

public static class VisualExtensions
{
    public static Point GetMousePoint(this IInputElement el)
    => Mouse.GetPosition(el);
    public static Point GetMouseScreenPoint<T>(this T visual)
        where T : Visual, IInputElement
        => visual.PointToScreen(visual.GetMousePoint());
}
