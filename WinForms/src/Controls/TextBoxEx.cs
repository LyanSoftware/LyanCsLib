using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Text;
using Windows.Win32;
using Timer = System.Windows.Forms.Timer;
using System.Runtime.InteropServices;

namespace Lytec.WinForms
{
    [DefaultEvent(nameof(TextChangeSuccess))]
    public class TextBoxEx : TextBox
    {
        [Browsable(true)]
        [DefaultValue("")]
        public string PlaceHolder
        {
            get => _PlaceHolder;
            set
            {
                _PlaceHolder = value;
                Invalidate();
            }
        }
        private string _PlaceHolder = "";
        [Browsable(true)]
        [DefaultValue(typeof(Color), "Gray")]
        public Color PlaceHolderColor { get; set; } = Color.Gray;
        [Browsable(true)]
        [DefaultValue(typeof(Point), "0, 0")]
        public Point PlaceHolderPosition { get; set; } = new Point(0, 0);

        [Browsable(false)]
        public Func<string, bool>? TextValidator { get; set; }

        [Browsable(true)]
        public event EventHandler? Edited;

        public delegate void TextChangeHandler(TextBoxEx sender, string value);

        [Browsable(true)]
        public event TextChangeHandler? TextChangeSuccess;

        [DefaultValue(typeof(BorderStyle), nameof(BorderStyle.FixedSingle))]
        public new BorderStyle BorderStyle { get => base.BorderStyle; set => base.BorderStyle = value; }

        //public new bool WordWrap
        //{
        //    get => base.WordWrap;
        //    set
        //    {
        //        if (value == WordWrap)
        //            return;
        //        base.WordWrap = value;
        //        this.SetDefaultWordbreak(value);
        //    }
        //}

        [Browsable(false)]
        public int MaxBytes { get; protected set; }

        [Browsable(false)]
        public Encoding? Encoding { get; protected set; }

        protected string TextCache { get; set; } = "";

        protected Timer TextChangedTimer { get; set; } = new() { Interval = 20 };
        public TextBoxEx()
        {
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BorderStyle = BorderStyle.FixedSingle;
            TextChanged += (sender, e) => Invalidate();
            TextChangedTimer.Tick += (sender, args) =>
            {
                TextChangedTimer.Stop();
                TextCache = Text;
                if (!IsDisposed)
                    TextChangeSuccess?.Invoke(this, Text);
            };
        }

        public void ResetMaxBytes() => MaxBytes = 0;

        public void SetMaxBytes(int maxBytes, Encoding encoding)
        {
            MaxBytes = maxBytes;
            Encoding = encoding;
        }

        public bool ValidateLength(string str)
        => MaxBytes < 1 || MaxBytes >= int.MaxValue || Encoding == default || (Encoding.GetByteCount(str) <= MaxBytes);

        public bool Validate(string str) => ValidateLength(str) && (TextValidator?.Invoke(str) ?? true);

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            switch (m.Msg)
            {
                case (int)PInvoke.WM_PAINT:
                case (int)PInvoke.WM_CTLCOLOREDIT:
                    using (var g = Graphics.FromHwnd(Handle))
                        OnPaint(new PaintEventArgs(g, ClientRectangle));
                    break;
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            var st = SelectionStart;
            var len = SelectionLength;
            if (Validate(Text))
            {
                TextChangedTimer.Restart();
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
                    //switch (chr)
                    if (chr >= '\x0020')
                    {
                        //case '\x01':    // Select All
                        //case '\x03':    // Copy
                        //case '\x18':    // Cut
                        //case '\x16':    // Paste
                        //case '\b':      // Backspace
                        //case '\x19':    // Redo
                        //case '\x1a':    // Undo
                        //case '\x1b':    // Escape
                        //    break;
                        //default:
                            var selst = SelectionStart;
                            var newstr = Text.Remove(selst, SelectionLength).Insert(selst, chr.ToString());
                            if (!Validate(newstr))
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
            if (ReadOnly) return;
            switch (e.KeyChar)
            {
                case '\r':
                case '\n':
                    if (Multiline)
                        break;
                    else goto case '\x1b';
                case '\x1b':
                    Edited?.Invoke(this, e);
                    break;
            }
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            if (ReadOnly) return;
            Edited?.Invoke(this, e);
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            if (ReadOnly) return;
            Edited?.Invoke(this, e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (string.IsNullOrEmpty(Text) && PlaceHolder.Length > 0)
                using (var brush = new SolidBrush(PlaceHolderColor))
                    e.Graphics.DrawString(PlaceHolder, Font, brush, Padding.Left + 2 + PlaceHolderPosition.X, Padding.Top + 2 + PlaceHolderPosition.Y);
        }

    }
}
