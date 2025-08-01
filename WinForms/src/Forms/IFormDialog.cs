using System;
using System.Windows.Forms;

namespace Lytec.WinForms
{
    public interface IFormDialog
    {
        DialogResult DialogResult { get; }
        void Show();
        void Show(IWin32Window owner);
        DialogResult ShowDialog();
        DialogResult ShowDialog(IWin32Window owner);
    }
}
