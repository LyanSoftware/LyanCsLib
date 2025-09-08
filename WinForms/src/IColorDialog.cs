using System.Drawing;
using System.Windows.Forms;

namespace Lytec.WinForms;

public interface IColorDialog
{
    Color Color { get; }

    DialogResult ShowDialog();
}
