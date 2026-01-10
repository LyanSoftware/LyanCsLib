using Avalonia.Styling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Markup.Declarative;

public static class LyStylePropertyExtensions
{
    public static Style<TElement> Setter<TElement, TValue>(this Style<TElement> style, StyledProperty<TValue> property, TValue value) where TElement : StyledElement
    {
        style.Setters.Add(new Setter(property, value));
        return style;
    }

    public static Style Setter<TValue>(this Style style, StyledProperty<TValue> property, TValue value)
    {
        style.Setters.Add(new Setter(property, value));
        return style;
    }

}
