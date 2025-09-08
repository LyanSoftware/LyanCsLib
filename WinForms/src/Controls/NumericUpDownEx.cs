using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace Lytec.WinForms
{
    public class NumericUpDownEx : NumericUpDown, ISupportInitialize
    {
        [Browsable(true)]
        [DefaultValue("")]
        public virtual string TextPrefix
        {
            get => _TextPrefix;
            set
            {
                _TextPrefix = value;
                UpdateEditText();
            }
        }
        private string _TextPrefix = "";

        [Browsable(true)]
        [DefaultValue("")]
        public virtual string TextSuffix
        {
            get => _TextSuffix;
            set
            {
                _TextSuffix = value;
                UpdateEditText();
            }
        }
        private string _TextSuffix = "";

        [Browsable(false)]
        //[EditorBrowsable(EditorBrowsableState.Never)]
        public new int DecimalPlaces
        {
            get
            {
                var str = (ValueWidth % 1).ToString();
                return str.Length > 2 ? Convert.ToInt32(str[2..]) : 0;
            }
            set => ValueWidth = (int)ValueWidth + Convert.ToSingle("0." + value);
        }

        [Browsable(true)]
        [DefaultValue(0.0f)]
        public virtual float ValueWidth
        {
            get => _ValueWidth;
            set
            {
                _ValueWidth = value;
                UpdateEditText();
            }
        }
        private float _ValueWidth;

        [Browsable(true)]
        [DefaultValue(typeof(HorizontalAlignment), nameof(HorizontalAlignment.Center))]
        public virtual new HorizontalAlignment TextAlign
        {
            get => base.TextAlign;
            set => base.TextAlign = value;
        }

        public NumericUpDownEx() => TextAlign = HorizontalAlignment.Center;

        public override void UpButton()
        {
            if (ReadOnly)
                return;
            base.UpButton();
        }

        public override void DownButton()
        {
            if (ReadOnly)
                return;
            base.DownButton();
        }

        protected override void OnLeave(EventArgs e)
        {
            base.OnLeave(e);
            OnValueChanged(new EventArgs());
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (ReadOnly)
                return;
            Value = Math.Min(Math.Max(Value + (e.Delta > 0 ? Increment : -Increment), Minimum), Maximum);
        }

        protected override void ValidateEditText()
        {
            var prefix = TextPrefix;
            var suffix = TextSuffix;
            if (!string.IsNullOrEmpty(prefix) || !string.IsNullOrEmpty(suffix))
            {
                var txt = Text;
                if (!string.IsNullOrEmpty(txt))
                {
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        if (txt.StartsWith(prefix))
                            txt = txt[prefix.Length..];
                        else if (!string.IsNullOrWhiteSpace(prefix))
                        {
                            prefix = prefix.Trim();
                            if (txt.StartsWith(prefix))
                                txt = txt[prefix.Length..];
                        }
                    }
                    if (!string.IsNullOrEmpty(suffix))
                    {
                        if (txt.EndsWith(suffix))
                            txt = txt[..^suffix.Length];
                        else if (!string.IsNullOrWhiteSpace(suffix))
                        {
                            suffix = suffix.Trim();
                            if (txt.EndsWith(suffix))
                                txt = txt[..^suffix.Length];
                        }
                    }
                    try
                    {
                        var value = Hexadecimal ? Convert.ToUInt64(txt, 16) : Convert.ToDecimal(txt);
                        value = Math.Max(value, Minimum);
                        value = Math.Min(value, Maximum);
                        Value = value;
                    }
                    catch { }
                }
                UserEdit = false;
                UpdateEditText();
            }
            else base.ValidateEditText();
        }

        protected override void UpdateEditText()
        {
            if (!Hexadecimal)
            {
                var format = new string(Enumerable.Repeat('0', (int)ValueWidth).ToArray());
                var fp = ValueWidth.ToString().Split('.');
                if (fp.Length > 1)
                    format = $"{format}.{new string(Enumerable.Repeat('0', Convert.ToInt32(fp[1])).ToArray())}";
                Text = $"{TextPrefix ?? ""}{(Hexadecimal ? ((ulong)Value).ToString($"X{(uint)ValueWidth}") : Value.ToString(format))}{TextSuffix ?? ""}";
            }
            else Text = $"{TextPrefix ?? ""}{((ulong)Value).ToString($"X{(uint)ValueWidth}")}{TextSuffix ?? ""}";
        }
    }
}
