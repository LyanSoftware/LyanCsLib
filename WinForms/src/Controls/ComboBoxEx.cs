using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Win32;

namespace Lytec.WinForms
{
    public class ComboBoxEx : ComboBox
    {
        [Browsable(false)]
        public Func<string, bool>? TextValidator { get; set; }

        [Browsable(true)]
        public event EventHandler? Edited;

        [Browsable(true)]
        public event Action<ComboBoxEx, string>? TextChangeSuccess;

        protected string TextCache { get; set; }

        public ComboBoxEx()
        {
            TextCache = SelectedText;
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            var (st, len) = (SelectionStart, SelectionLength);
            if (TextValidator?.Invoke(Text) ?? true)
            {
                TextCache = Text;
                TextChangeSuccess?.Invoke(this, Text);
            }
            else
            {
                Text = TextCache;
                SelectionStart = st;
                SelectionLength = len;
            }
        }

        protected override bool ProcessKeyMessage(ref Message m)
        {
            switch (m.Msg)
            {
                case (int)PInvoke.WM_CHAR:
                case (int)PInvoke.WM_SYSCHAR:
                    var chr = (char)m.WParam;
                    switch (chr)
                    {
                        case '\x01':    // Select All
                        case '\x03':    // Copy
                        case '\b':      // Backspace
                        case '\x1b':    // Escape
                            break;
                        default:
                            var selst = SelectionStart;
                            var newstr = Text.Remove(selst, SelectionLength).Insert(selst, chr.ToString());
                            if (!(TextValidator?.Invoke(newstr) ?? true))
                                return true; // Block Invalid Input
                            else break;
                    }
                    break;
                default:
                    break;
            }
            return base.ProcessKeyMessage(ref m);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            switch (e.KeyChar)
            {
                case '\r':
                case '\n':
                case '\b':
                case '\x1b':
                    Edited?.Invoke(this, e);
                    break;
            }
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            Edited?.Invoke(this, e);
        }
    }
}
