using Avalonia;
using Avalonia.Markup.Declarative;

namespace Lytec.AvaloniaUI.Mvu;

#if NET10_0_OR_GREATER

public class MvuBase() : ViewBase<object>(new object())
{
    protected override object Build(object vm)
    {
        throw new NotImplementedException();
    }
}

#endif
