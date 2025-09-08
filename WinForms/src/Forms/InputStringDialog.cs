using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lytec.WinForms
{
    public partial class InputStringDialog : Dialog
    {
        public string Value
        {
            get => InputBox.Text;
            set => InputBox.Text = value;
        }

        public string Title
        {
            get => Text;
            set => Text = value;
        }

        public string TipText
        {
            get => Tip.Text;
            set => Tip.Text = value;
        }

        public string OKButtonText
        {
            get => OKButton.Text;
            set => OKButton.Text = value;
        }

        public string CancelButtonText
        {
            get => CancelButton.Text;
            set => CancelButton.Text = value;
        }

        public bool Multiline
        {
            get => InputBox.Multiline;
            set
            {
                InputBox.Multiline = value;
                if (value)
                    InputBox.Anchor |= AnchorStyles.Top | AnchorStyles.Bottom;
                else InputBox.Anchor &= ~(AnchorStyles.Top | AnchorStyles.Bottom);
            }
        }

        public int MaxLength
        {
            get => InputBox.MaxLength;
            set => InputBox.MaxLength = value;
        }

        public int MaxBytes => InputBox.MaxBytes;
        public Encoding? Encoding => InputBox.Encoding;

        public bool IsBusy { get => !OKButton.Enabled; set => OKButton.Enabled = !value; }

        public Func<string, Task>? Validator { get; set; }

        public Func<string, bool>? InputingValidator
        {
            get => InputBox.TextValidator;
            set => InputBox.TextValidator = value;
        }

        public InputStringDialog(bool multiline = false, int maxLength = 32767)
        {
            InitializeComponent();
            base.CancelButton = CancelButton;
            Multiline = multiline;
            MaxLength = maxLength;
        }

        public InputStringDialog(int maxBytes, Encoding encoding, bool multiline = false) : this(multiline) => SetMaxBytes(maxBytes, encoding);

        public void ResetMaxBytes() => InputBox.ResetMaxBytes();

        public void SetMaxBytes(int maxBytes, Encoding encoding) => InputBox.SetMaxBytes(maxBytes, encoding);

        /// <summary>
        /// 设置界面文本后将窗体显示为模态对话框。
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="tiptext">提示标签文本</param>
        /// <param name="oktext">确定按钮文本</param>
        /// <param name="canceltext">取消按钮文本</param>
        /// <param name="value">默认值</param>
        /// <param name="restoreOnClose">在对话框关闭后恢复修改</param>
        /// <returns><see cref="DialogResult.OK"/> 或 <see cref="DialogResult.Cancel"/></returns>
        public virtual DialogResult ShowDialog(string title, string tiptext, string oktext, string canceltext, string value, bool restoreOnClose = false)
        {
            DialogResult ret;
            var (ti, tip, ok, cancel, v) = (Title, TipText, OKButtonText, CancelButtonText, Value);
            (Title, TipText, OKButtonText, CancelButtonText, Value) = (title ?? Title, tiptext ?? TipText, oktext ?? OKButtonText, canceltext ?? CancelButtonText, value ?? Value);
            ret = ShowDialog();
            if (restoreOnClose)
                (Title, TipText, OKButtonText, CancelButtonText, Value) = (ti, tip, ok, cancel, v);
            return ret;
        }

        private async void OKButton_Click(object sender, EventArgs e)
        {
            if (Validator != null)
            {
                try
                {
                    IsBusy = true;
                    await Task.Run(async () => await Validator.Invoke(Value));
                }
                catch (Exception err)
                {
                    this.ErrBox(err.Message);
                    return;
                }
                finally
                {
                    IsBusy = false;
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
