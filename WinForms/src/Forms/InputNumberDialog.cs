using System.Windows.Forms;

namespace Lytec.WinForms
{
    public partial class InputNumberDialog : Dialog
    {
        public decimal Value
        {
            get => NumberInput.Value;
            set => NumberInput.Value = value;
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
            get => CancelButton1.Text;
            set => CancelButton1.Text = value;
        }

        public bool Hexadecimal
        {
            get => NumberInput.Hexadecimal;
            set => NumberInput.Hexadecimal = value;
        }

        public string InputPrefix
        {
            get => NumberInput.TextPrefix;
            set => NumberInput.TextPrefix = value;
        }

        public string InputSuffix
        {
            get => NumberInput.TextSuffix;
            set => NumberInput.TextSuffix = value;
        }

        public InputNumberDialog(decimal min = 0, decimal max = int.MaxValue)
        {
            InitializeComponent();
            base.CancelButton = CancelButton1;
            NumberInput.Minimum = min;
            NumberInput.Maximum = max;
        }

        /// <summary>
        /// 设置界面文本后将窗体显示为模态对话框。
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="tiptext">提示标签文本</param>
        /// <param name="oktext">确定按钮文本</param>
        /// <param name="canceltext">取消按钮文本</param>
        /// <param name="defaultValue">默认数值</param>
        /// <param name="restoreOnClose">在对话框关闭后恢复修改</param>
        /// <returns><see cref="DialogResult.OK"/> 或 <see cref="DialogResult.Cancel"/></returns>
        public virtual DialogResult ShowDialog(string title, string tiptext, string oktext, string canceltext, decimal defaultValue = 0, bool restoreOnClose = false)
        {
            DialogResult ret;
            var (ti, tip, ok, cancel, value) = (Title, TipText, OKButtonText, CancelButtonText, Value);
            (Title, TipText, OKButtonText, CancelButtonText, Value) = (title ?? Title, tiptext ?? TipText, oktext ?? OKButtonText, canceltext ?? CancelButtonText, defaultValue);
            ret = ShowDialog();
            if (restoreOnClose)
                (Title, TipText, OKButtonText, CancelButtonText, Value) = (ti, tip, ok, cancel, value);
            return ret;
        }
    }
}
