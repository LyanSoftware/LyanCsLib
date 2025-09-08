using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Lytec.Common.Communication;
using static System.Globalization.NumberStyles;

#nullable disable

namespace Lytec.WinForms
{

    [DefaultEvent(nameof(ValueChanged))]
    public class MacAddressBox : UserControl
    {
        [Browsable(true)]
        [DefaultValue("-")]
        public string Separator
        {
            get => label1.Text;
            set
            {
                label1.Text = value;
                label2.Text = value;
                label3.Text = value;
                label4.Text = value;
                label5.Text = value;
            }
        }

        [Browsable(true)]
        [DefaultValue(20)]
        public int InputAreaWidth
        {
            get => FirstInput.Width;
            set
            {
                foreach (var input in Inputs)
                    input.Width = value;
            }
        }

        [DefaultValue(typeof(BorderStyle), nameof(BorderStyle.FixedSingle))]
        public new BorderStyle BorderStyle { get => base.BorderStyle; set => base.BorderStyle = value; }

        [Browsable(true)]
        [DefaultValue(typeof(MacAddress), "000000000000")]
        public MacAddress MacAddress
        {
            get => _MacAddress;
            set
            {
                if (_MacAddress != value)
                {
                    _MacAddress = value;
                    Invalidate();
                }
            }
        }
        private MacAddress _MacAddress;

        public MacAddress Value { get => MacAddress; set => MacAddress = value; }

        private TextBoxEx[] Inputs { get; }

        [Browsable(true)]
        [DefaultValue(false)]
        public bool ReadOnly
        {
            get => FirstInput.ReadOnly;
            set
            {
                if (value == FirstInput.ReadOnly) return;
                foreach (var input in Inputs)
                    input.ReadOnly = value;
                if (value)
                    RestoreBackColor = BackColor;
                BackColor = value ? ReadOnlyBackColor : RestoreBackColor;
                Invalidate();
            }
        }
        private Color RestoreBackColor { get; set; }

        [Browsable(true)]
        [DefaultValue(typeof(Color), nameof(Color.Gainsboro))]
        public Color ReadOnlyBackColor { get; set; } = Color.Gainsboro;

        [Browsable(true)]
        [DefaultValue(typeof(Color), nameof(Color.Transparent))]
        public override Color BackColor
        {
            get => base.BackColor;
            set
            {
                label1.BackColor = value;
                label2.BackColor = value;
                label3.BackColor = value;
                label4.BackColor = value;
                label5.BackColor = value;
                foreach (var input in Inputs)
                    input.BackColor = value;
                base.BackColor = value;
                Invalidate();
            }
        }

        [Browsable(true)]
        public event Action<MacAddressBox, MacAddress> ValueChanged;

        public TextBoxEx FirstInput => Inputs[0];
        public TextBoxEx LastInput => Inputs[^1];

        public MacAddressBox()
        {
            InitializeComponent();
            Inputs = new TextBoxEx[] { ByteInput1!, ByteInput2!, ByteInput3!, ByteInput4!, ByteInput5!, ByteInput6! };
            BackColor = Color.Transparent;
            Separator = "-";
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
                Inputs[i].TextValidator += str => string.IsNullOrEmpty(str) || (!str.StartsWith("-") && byte.TryParse(str, AllowHexSpecifier, null, out _));
                Inputs[i].KeyDown += (sender, args) =>
                {
                    if (ReadOnly) return;
                    switch (args.KeyCode)
                    {
                        case Keys.C: // 复制
                            if (args.Modifiers.HasFlag(Keys.Control))
                                Clipboard.SetText(Value.ToString());
                            break;
                        case Keys.V: // 粘贴
                            if (args.Modifiers.HasFlag(Keys.Control))
                            {
                                args.SuppressKeyPress = true;
                                var str = Clipboard.GetText();
                                if (MacAddress.TryParse(str, out var mac))
                                {
                                    UpdateData(mac, true);
                                    LastInput.Focus();
                                    LastInput.Select(LastInput.Text.Length, 0);
                                }
                            }
                            break;
                        case Keys.Back: // 退格
                            if (sender != FirstInput && Inputs[inputindex].Text.Length < 1)
                                prev();
                            break;
                        case Keys.Subtract:
                        case Keys.OemMinus:
                            next();
                            break;
                        case Keys.OemSemicolon:
                            if (args.Modifiers.HasFlag(Keys.Shift))
                                next();
                            break;
                    }
                };
                Inputs[i].KeyPress += async (sender, args) =>
                {
                    if ((args.KeyChar >= 'A' && args.KeyChar <= 'F')
                    || (args.KeyChar >= 'a' && args.KeyChar <= 'f')
                    || (args.KeyChar >= '0' && args.KeyChar <= '9'))
                    {
                        var input = sender as TextBox;
                        if (input!.Text == "00")
                            input.Text = "";
                        await Task.Delay(1);
                        if (sender == LastInput) return;
                        else if (input.Text.Length == 2 && input.SelectionStart == 2) next();
                    }
                };
                Inputs[i].TextChangeSuccess += (sender, str) =>
                {
                    var input = sender as TextBox;
                    var val = byte.TryParse(input.Text, AllowHexSpecifier, null, out var b) ? b : (byte)0;
                    var mac = MacAddress;
                    mac[input.Name.Last() - '0' - 1] = val;
                    MacAddress = mac;
                    ValueChanged?.Invoke(this, mac);
                };
                Inputs[i].Edited += (sender, str) =>
                {
                    var input = sender as Control;
                    while (input!.Text.Length < 2)
                        input.Text = '0' + input.Text;
                };
            }
        }

        public void UpdateData() => UpdateData(MacAddress);
        public void UpdateData(MacAddress mac, bool forceUpdate = false)
        {
            for (var i = 0; i < Inputs.Length; i++)
                if (forceUpdate || !Inputs[i].Focused)
                    Inputs[i].Text = mac[i].ToString("X2");
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            UpdateData();
            base.OnPaint(e);
        }

        private TableLayoutPanel MainLayout;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private TextBoxEx ByteInput1;
        private TextBoxEx ByteInput2;
        private TextBoxEx ByteInput3;
        private TextBoxEx ByteInput4;
        private TextBoxEx ByteInput5;
        private TextBoxEx ByteInput6;



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
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.ByteInput1 = new WinForms.TextBoxEx();
            this.ByteInput2 = new WinForms.TextBoxEx();
            this.ByteInput3 = new WinForms.TextBoxEx();
            this.ByteInput4 = new WinForms.TextBoxEx();
            this.ByteInput5 = new WinForms.TextBoxEx();
            this.ByteInput6 = new WinForms.TextBoxEx();
            this.MainLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // MainLayout
            // 
            this.MainLayout.AutoSize = true;
            this.MainLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.MainLayout.ColumnCount = 11;
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.MainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.66667F));
            this.MainLayout.Controls.Add(this.label1, 1, 0);
            this.MainLayout.Controls.Add(this.label2, 3, 0);
            this.MainLayout.Controls.Add(this.label3, 5, 0);
            this.MainLayout.Controls.Add(this.label4, 7, 0);
            this.MainLayout.Controls.Add(this.label5, 9, 0);
            this.MainLayout.Controls.Add(this.ByteInput1, 0, 0);
            this.MainLayout.Controls.Add(this.ByteInput2, 2, 0);
            this.MainLayout.Controls.Add(this.ByteInput3, 4, 0);
            this.MainLayout.Controls.Add(this.ByteInput4, 6, 0);
            this.MainLayout.Controls.Add(this.ByteInput5, 8, 0);
            this.MainLayout.Controls.Add(this.ByteInput6, 10, 0);
            this.MainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.MainLayout.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.MainLayout.Location = new System.Drawing.Point(0, 0);
            this.MainLayout.Margin = new System.Windows.Forms.Padding(0);
            this.MainLayout.Name = "MainLayout";
            this.MainLayout.RowCount = 1;
            this.MainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.MainLayout.Size = new System.Drawing.Size(291, 23);
            this.MainLayout.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Location = new System.Drawing.Point(39, 5);
            this.label1.Margin = new System.Windows.Forms.Padding(0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(11, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "-";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Location = new System.Drawing.Point(89, 5);
            this.label2.Margin = new System.Windows.Forms.Padding(0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(11, 12);
            this.label2.TabIndex = 1;
            this.label2.Text = "-";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Location = new System.Drawing.Point(139, 5);
            this.label3.Margin = new System.Windows.Forms.Padding(0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(11, 12);
            this.label3.TabIndex = 2;
            this.label3.Text = "-";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label4
            // 
            this.label4.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.Color.Transparent;
            this.label4.Location = new System.Drawing.Point(189, 5);
            this.label4.Margin = new System.Windows.Forms.Padding(0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(11, 12);
            this.label4.TabIndex = 3;
            this.label4.Text = "-";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label5
            // 
            this.label5.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.Color.Transparent;
            this.label5.Location = new System.Drawing.Point(239, 5);
            this.label5.Margin = new System.Windows.Forms.Padding(0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(11, 12);
            this.label5.TabIndex = 4;
            this.label5.Text = "-";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // MacByteInput1
            // 
            this.ByteInput1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.ByteInput1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ByteInput1.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.ByteInput1.Location = new System.Drawing.Point(0, 4);
            this.ByteInput1.Margin = new System.Windows.Forms.Padding(0);
            this.ByteInput1.MaxLength = 2;
            this.ByteInput1.Name = "MacByteInput1";
            this.ByteInput1.Size = new System.Drawing.Size(39, 14);
            this.ByteInput1.TabIndex = 5;
            this.ByteInput1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ByteInput1.TextValidator = null;
            // 
            // MacByteInput2
            // 
            this.ByteInput2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.ByteInput2.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ByteInput2.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.ByteInput2.Location = new System.Drawing.Point(50, 4);
            this.ByteInput2.Margin = new System.Windows.Forms.Padding(0);
            this.ByteInput2.MaxLength = 2;
            this.ByteInput2.Name = "MacByteInput2";
            this.ByteInput2.Size = new System.Drawing.Size(39, 14);
            this.ByteInput2.TabIndex = 6;
            this.ByteInput2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ByteInput2.TextValidator = null;
            // 
            // MacByteInput3
            // 
            this.ByteInput3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.ByteInput3.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ByteInput3.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.ByteInput3.Location = new System.Drawing.Point(100, 4);
            this.ByteInput3.Margin = new System.Windows.Forms.Padding(0);
            this.ByteInput3.MaxLength = 2;
            this.ByteInput3.Name = "MacByteInput3";
            this.ByteInput3.Size = new System.Drawing.Size(39, 14);
            this.ByteInput3.TabIndex = 7;
            this.ByteInput3.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ByteInput3.TextValidator = null;
            // 
            // MacByteInput4
            // 
            this.ByteInput4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.ByteInput4.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ByteInput4.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.ByteInput4.Location = new System.Drawing.Point(150, 4);
            this.ByteInput4.Margin = new System.Windows.Forms.Padding(0);
            this.ByteInput4.MaxLength = 2;
            this.ByteInput4.Name = "MacByteInput4";
            this.ByteInput4.Size = new System.Drawing.Size(39, 14);
            this.ByteInput4.TabIndex = 8;
            this.ByteInput4.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ByteInput4.TextValidator = null;
            // 
            // MacByteInput5
            // 
            this.ByteInput5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.ByteInput5.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ByteInput5.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.ByteInput5.Location = new System.Drawing.Point(200, 4);
            this.ByteInput5.Margin = new System.Windows.Forms.Padding(0);
            this.ByteInput5.MaxLength = 2;
            this.ByteInput5.Name = "MacByteInput5";
            this.ByteInput5.Size = new System.Drawing.Size(39, 14);
            this.ByteInput5.TabIndex = 9;
            this.ByteInput5.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ByteInput5.TextValidator = null;
            // 
            // MacByteInput6
            // 
            this.ByteInput6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.ByteInput6.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ByteInput6.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.ByteInput6.Location = new System.Drawing.Point(250, 4);
            this.ByteInput6.Margin = new System.Windows.Forms.Padding(0);
            this.ByteInput6.MaxLength = 2;
            this.ByteInput6.Name = "MacByteInput6";
            this.ByteInput6.Size = new System.Drawing.Size(41, 14);
            this.ByteInput6.TabIndex = 10;
            this.ByteInput6.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ByteInput6.TextValidator = null;
            // 
            // MacAddressBox
            // 
            this.Controls.Add(this.MainLayout);
            this.Name = "MacAddressBox";
            this.Size = new System.Drawing.Size(291, 23);
            this.MainLayout.ResumeLayout(false);
            this.MainLayout.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
    }
}

#nullable restore
