using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Lytec.Common.Converters;
using Lytec.Common.Data;
using Newtonsoft.Json.Linq;

#nullable disable

namespace Lytec.WinForms
{
    [DefaultEvent(nameof(ValueChanged))]
    public class IPv4AddressBox : UserControl
    {
        [Browsable(true)]
        [DefaultValue(20)]
        public int InputAreaWidth
        {
            get => ByteInput1.Width;
            set
            {
                foreach (var input in Inputs)
                    input.Width = value;
            }
        }

        [Browsable(true)]
        //[DefaultValue(typeof(IPAddress), "0")]
        [TypeConverter(typeof(StringTypeConverter<IPAddress>))]
        public virtual IPAddress IPAddress { get => Value; set => Value = value; }

        [Browsable(true)]
        //[DefaultValue(typeof(IPAddress), "0")]
        [TypeConverter(typeof(StringTypeConverter<IPAddress>))]
        public virtual IPAddress Value
        {
            get => _Value;
            set
            {
                _Value = value;
                UpdateData();
            }
        }
        private IPAddress _Value = new IPAddress(0);

        protected virtual IPAddress DefaultValue => new IPAddress(0);

        private TextBoxEx[] Inputs { get; }

        [Browsable(true)]
        [DefaultValue(false)]
        public bool ReadOnly
        {
            get => _ReadOnly;
            set
            {
                if (value == ReadOnly) return;
                _ReadOnly = value;
                foreach (var input in Inputs)
                    input.ReadOnly = value;
                UpdateBackColor();
            }
        }
        private bool _ReadOnly = false;

        [Browsable(true)]
        [DefaultValue(typeof(Color), nameof(Color.WhiteSmoke))]
        public virtual Color ReadOnlyBackColor { get; set; } = Color.WhiteSmoke;

        [Browsable(true)]
        [DefaultValue(typeof(Color), nameof(Color.Transparent))]
        public override Color BackColor
        {
            get => base.BackColor;
            set
            {
                if (Enabled && !ReadOnly)
                    NormalBackColorCache = value;
                if (BackColor == value)
                    return;
                base.BackColor = value;
                label1.BackColor = value;
                label2.BackColor = value;
                label3.BackColor = value;
                foreach (var input in Inputs)
                    input.BackColor = value;
                Invalidate();
            }
        }

        private Color NormalBackColorCache { get; set; } = Color.Transparent;

        [Browsable(true)]
        [DefaultValue(typeof(Color), nameof(Color.WhiteSmoke))]
        public virtual Color DisabledBackColor { get; set; } = Color.WhiteSmoke;

        [Browsable(true)]
        [DefaultValue(typeof(BorderStyle), nameof(BorderStyle.FixedSingle))]
        public virtual new BorderStyle BorderStyle { get => base.BorderStyle; set => base.BorderStyle = value; }

        protected override Padding DefaultPadding => new Padding(3);

        public delegate void ValueChangeHandler(IPv4AddressBox sender, IPAddress value);

        [Browsable(true)]
        public event ValueChangeHandler ValueChanged;

        public TextBoxEx FirstInput => Inputs[0];
        public TextBoxEx LastInput => Inputs[^1];

        public IPv4AddressBox()
        {
            InitializeComponent();
            Inputs = new TextBoxEx[] { ByteInput1!, ByteInput2!, ByteInput3!, ByteInput4! };
            BackColor = Color.Transparent;
            InputAreaWidth = 20;
            BorderStyle = BorderStyle.FixedSingle;
            for (var i = 0; i < Inputs.Length; i++)
            {
                var inputindex = i;
                void prev() => Inputs[inputindex - 1].Focus();
                void next()
                {
                    Inputs[inputindex + 1].Focus();
                    Inputs[inputindex + 1].SelectAll();
                }
                Inputs[i].TextValidator += str => string.IsNullOrEmpty(str) || (!str.StartsWith("-") && byte.TryParse(str, out _));
                Inputs[i].KeyDown += (sender, args) =>
                {
                    switch (args.KeyCode)
                    {
                        case Keys.C: // 复制
                            if (args.Modifiers.HasFlag(Keys.Control))
                            {
                                Clipboard.SetText(Value.ToString());
                            }
                            break;
                        case Keys.V: // 粘贴
                            if (ReadOnly) return;
                            if (args.Modifiers.HasFlag(Keys.Control))
                            {
                                args.SuppressKeyPress = true;
                                var str = Clipboard.GetText();
                                if (IPAddress.TryParse(str, out var ip) && ip.AddressFamily == AddressFamily.InterNetwork)
                                {
                                    UpdateData(ip, true);
                                    LastInput.Focus();
                                    LastInput.Select(LastInput.Text.Length, 0);
                                }
                            }
                            break;
                        case Keys.Back: // 退格
                            if (ReadOnly) return;
                            if (sender != FirstInput && Inputs[inputindex].Text.Length < 1)
                                prev();
                            break;
                        case Keys.Decimal:
                        case Keys.OemPeriod:
                        //case Keys.Subtract:
                        //case Keys.OemMinus:
                            if (ReadOnly) return;
                            else goto case Keys.Right;
                        case Keys.Right:
                            if (sender != LastInput)
                                next();
                            break;
                        case Keys.Left:
                            if (sender != FirstInput)
                                prev();
                            break;
                    }
                };
                Inputs[i].KeyPress += (sender, args) =>
                {
                    if (args.KeyChar >= '0' && args.KeyChar <= '9')
                    {
                        var input = sender as TextBox;
                        if (input!.Text == "0")
                            input.Text = "";
                        //await Task.Delay(1);
                        //if (sender == LastInput) return;
                        //else if (input.Text.Length == 3 && input.SelectionStart == 3) next();
                    }
                };
                Inputs[i].TextChangeSuccess += (sender, str) =>
                {
                    var input = sender as TextBox;
                    var val = byte.TryParse(input.Text, out var b) ? b : (byte)0;
                    var bytes = Value.GetAddressBytes();
                    bytes[input.Name.Last() - '0' - 1] = val;
                    Value = new IPAddress(bytes);
                    ValueChanged?.Invoke(this, new IPAddress(bytes));
                };
                Inputs[i].Edited += (sender, str) =>
                {
                    var input = sender as Control;
                    if (byte.TryParse(input!.Text, out var d))
                        input.Text = d.ToString();
                    else input.Text = "0";
                };
            }
        }

        public virtual void UpdateBackColor()
        {
            var color = NormalBackColorCache;
            if (!Enabled)
                color = DisabledBackColor;
            else if (ReadOnly)
                color = ReadOnlyBackColor;
            BackColor = color;
            Invalidate();
        }

        public void SetAddress(IPAddress addr)
        {
            Value = addr;
            UpdateData(true);
        }
        public void SetAddress(int addr) => SetAddress(new IPAddress(addr.ToBytes(Endian.Big)));

        protected override void OnEnabledChanged(EventArgs e)
        {
            UpdateBackColor();
            base.OnEnabledChanged(e);
        }

        public void UpdateData(bool forceUpdate = false) => UpdateData(Value, forceUpdate);
        public void UpdateData(IPAddress ip, bool forceUpdate = false)
        {
            if (ip.AddressFamily != AddressFamily.InterNetwork) throw new ArgumentException("Only IPv4 addresses are supported");
            var bytes = ip.GetAddressBytes();
            for (var i = 0; i < Inputs.Length; i++)
                if (forceUpdate || ReadOnly || !Inputs[i].Focused)
                    Inputs[i].Text = bytes[i].ToString();
        }

        private bool ShouldSerializeValue() => !Value.Equals(DefaultValue);
        private void ResetValue() => Value = DefaultValue;
        private bool ShouldSerializeIPAddress() => ShouldSerializeValue();
        private void ResetIPAddress() => ResetValue();

        protected override void OnPaint(PaintEventArgs e)
        {
            UpdateData();
            base.OnPaint(e);
        }

        private TableLayoutPanel MainLayout;
        private Label label1;
        private Label label2;
        private Label label3;
        private TextBoxEx ByteInput1;
        private TextBoxEx ByteInput2;
        private TextBoxEx ByteInput3;
        private TextBoxEx ByteInput4;


        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0044:添加只读修饰符", Justification = "<挂起>")]
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.MainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.ByteInput1 = new Lytec.WinForms.TextBoxEx();
            this.ByteInput2 = new Lytec.WinForms.TextBoxEx();
            this.ByteInput3 = new Lytec.WinForms.TextBoxEx();
            this.ByteInput4 = new Lytec.WinForms.TextBoxEx();
            this.MainLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainLayout
            // 
            this.MainLayout.AutoSize = true;
            this.MainLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.MainLayout.ColumnCount = 7;
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.MainLayout.Controls.Add(this.label1, 1, 0);
            this.MainLayout.Controls.Add(this.label2, 3, 0);
            this.MainLayout.Controls.Add(this.label3, 5, 0);
            this.MainLayout.Controls.Add(this.ByteInput1, 0, 0);
            this.MainLayout.Controls.Add(this.ByteInput2, 2, 0);
            this.MainLayout.Controls.Add(this.ByteInput3, 4, 0);
            this.MainLayout.Controls.Add(this.ByteInput4, 6, 0);
            this.MainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainLayout.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.MainLayout.Location = new System.Drawing.Point(0, 0);
            this.MainLayout.Margin = new System.Windows.Forms.Padding(0);
            this.MainLayout.Name = "MainLayout";
            this.MainLayout.RowCount = 1;
            this.MainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.MainLayout.Size = new System.Drawing.Size(201, 23);
            this.MainLayout.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Location = new System.Drawing.Point(42, 5);
            this.label1.Margin = new System.Windows.Forms.Padding(0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(11, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = ".";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Location = new System.Drawing.Point(95, 5);
            this.label2.Margin = new System.Windows.Forms.Padding(0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(11, 12);
            this.label2.TabIndex = 1;
            this.label2.Text = ".";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Location = new System.Drawing.Point(148, 5);
            this.label3.Margin = new System.Windows.Forms.Padding(0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(11, 12);
            this.label3.TabIndex = 2;
            this.label3.Text = ".";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ByteInput1
            // 
            this.ByteInput1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.ByteInput1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ByteInput1.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.ByteInput1.Location = new System.Drawing.Point(0, 5);
            this.ByteInput1.Margin = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.ByteInput1.MaxLength = 3;
            this.ByteInput1.Name = "ByteInput1";
            this.ByteInput1.Size = new System.Drawing.Size(42, 14);
            this.ByteInput1.TabIndex = 5;
            this.ByteInput1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ByteInput1.TextValidator = null;
            // 
            // ByteInput2
            // 
            this.ByteInput2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.ByteInput2.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ByteInput2.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.ByteInput2.Location = new System.Drawing.Point(53, 5);
            this.ByteInput2.Margin = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.ByteInput2.MaxLength = 3;
            this.ByteInput2.Name = "ByteInput2";
            this.ByteInput2.Size = new System.Drawing.Size(42, 14);
            this.ByteInput2.TabIndex = 6;
            this.ByteInput2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ByteInput2.TextValidator = null;
            // 
            // ByteInput3
            // 
            this.ByteInput3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.ByteInput3.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ByteInput3.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.ByteInput3.Location = new System.Drawing.Point(106, 5);
            this.ByteInput3.Margin = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.ByteInput3.MaxLength = 3;
            this.ByteInput3.Name = "ByteInput3";
            this.ByteInput3.Size = new System.Drawing.Size(42, 14);
            this.ByteInput3.TabIndex = 7;
            this.ByteInput3.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ByteInput3.TextValidator = null;
            // 
            // ByteInput4
            // 
            this.ByteInput4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.ByteInput4.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ByteInput4.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.ByteInput4.Location = new System.Drawing.Point(159, 5);
            this.ByteInput4.Margin = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.ByteInput4.MaxLength = 3;
            this.ByteInput4.Name = "ByteInput4";
            this.ByteInput4.Size = new System.Drawing.Size(42, 14);
            this.ByteInput4.TabIndex = 8;
            this.ByteInput4.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ByteInput4.TextValidator = null;
            // 
            // IPv4AddressBox
            // 
            this.Controls.Add(this.MainLayout);
            this.Name = "IPv4AddressBox";
            this.Size = new System.Drawing.Size(201, 23);
            this.MainLayout.ResumeLayout(false);
            this.MainLayout.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
    }
}

#nullable restore
