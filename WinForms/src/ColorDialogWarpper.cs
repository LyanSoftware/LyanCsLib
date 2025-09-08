using System.Drawing;
using System.Windows.Forms;

namespace Lytec.WinForms;

public struct ColorDialogWarpper : IColorDialog
{
    private readonly Func<Color> _GetColor;
    private readonly Func<DialogResult> _ShowDialog;

    public Color Color => _GetColor?.Invoke() ?? throw new NotImplementedException();

    public ColorDialogWarpper(Func<Color> getColor, Func<DialogResult> showDialog)
    {
        _GetColor = getColor;
        _ShowDialog = showDialog;
    }

    public DialogResult ShowDialog() => _ShowDialog?.Invoke() ?? throw new NotImplementedException();
}

public struct ColorDialogWarpper<T> : IColorDialog
{
    private readonly T _Dialog;
    private readonly Func<T, Color> _GetColor;
    private readonly Func<T, DialogResult> _ShowDialog;

    public Color Color => _GetColor?.Invoke(_Dialog) ?? throw new NotImplementedException();

    public ColorDialogWarpper(T dialog, Func<T, Color> getColor, Func<T, DialogResult> showDialog)
    {
        _Dialog = dialog;
        _GetColor = getColor;
        _ShowDialog = showDialog;
    }

    public DialogResult ShowDialog() => _ShowDialog?.Invoke(_Dialog) ?? throw new NotImplementedException();
}
