using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Lytec.Image;

partial struct Color
{
    public static Color Transparent { get; } = new(0, 0, 0, 0);
    public static Color Black { get; } = new(0, 0, 0);
    public static Color Red { get; } = new(255, 0, 0);
    public static Color Green { get; } = new(0, 255, 0);
    public static Color Blue { get; } = new(0, 0, 255);
    public static Color Cyan { get; } = new(0, 255, 255);
    public static Color Magenta { get; } = new(255, 0, 255);
    public static Color Yellow { get; } = new(255, 255, 0);
    public static Color White { get; } = new(255, 255, 255);

    public static IDictionary<string, Color> Colors { get; set; }
    = typeof(Color).GetProperties(BindingFlags.Public | BindingFlags.Static)
        .Where(p => p.PropertyType == typeof(Color))
        .ToDictionary(p => p.Name, p => (Color)p.GetValue(null)!, StringComparer.OrdinalIgnoreCase);
}
