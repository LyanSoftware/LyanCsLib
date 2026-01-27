using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Lytec.WinForms
{
    public partial class WaitDialog : Dialog
    {
        public string ButtonText { get => ButtonCancel.Text; set => ButtonCancel.Text = value; }

        public string Message { get => MessageLabel.Text; set => MessageLabel.Text = value; }

        public string Caption { get => Text; set => Text = value; }

        public string Title { get => Text; set => Text = value; }

        public event Action<WaitDialog>? OnCancel;

        public bool ButtonVisible { get => ButtonCancel.Visible; set => ButtonCancel.Visible = value; }

        public WaitDialog()
        {
            InitializeComponent();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.UserClosing)
            {
                e.Cancel = true;
                ButtonCancel.PerformClick();
            }
            else base.OnFormClosing(e);
        }

        public void MoveToCenterParent() => CenterToParent();

        public void Cancel() => OnCancel?.Invoke(this);

        private void ButtonCancel_Click(object sender, EventArgs e) => Cancel();
    }
}
