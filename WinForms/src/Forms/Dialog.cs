using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Lytec.WinForms
{
    public class Dialog : Form, IFormDialog
    {
        public enum OnEscKeyDownAction
        {
            Ignore = 0,
            Close = 1,
            Cancel = 2
        }

        [DefaultValue(true)]
        public new virtual bool KeyPreview { get => base.KeyPreview; set => base.KeyPreview = value; }

        [DefaultValue(false)]
        public new virtual bool ShowIcon { get => base.ShowIcon; set => base.ShowIcon = value; }

        [DefaultValue(false)]
        public new virtual bool ShowInTaskbar { get => base.ShowInTaskbar; set => base.ShowInTaskbar = value; }

        [DefaultValue(typeof(FormStartPosition), nameof(FormStartPosition.CenterParent))]
        public new virtual FormStartPosition StartPosition { get => base.StartPosition; set => base.StartPosition = value; }

        [Browsable(false)]
        public new virtual Color DefaultBackColor { get; } = Color.White;

        [DefaultValue(false)]
        public new bool MaximizeBox { get => base.MaximizeBox; set => base.MaximizeBox = value; }

        [DefaultValue(false)]
        public new bool MinimizeBox { get => base.MinimizeBox; set => base.MinimizeBox = value; }

        public virtual bool CloseOnEscKeyDown { get => OnEscKeyDown == OnEscKeyDownAction.Close; set => OnEscKeyDown = OnEscKeyDownAction.Close; }

        [DefaultValue(typeof(OnEscKeyDownAction), nameof(OnEscKeyDownAction.Ignore))]
        [Browsable(true)]
        public virtual OnEscKeyDownAction OnEscKeyDown { get; set; } = OnEscKeyDownAction.Ignore;

        private readonly bool IsInDesigner;

        public Dialog()
        {
            BackColor = DefaultBackColor;
            KeyPreview = true;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            IsInDesigner = LicenseManager.UsageMode == LicenseUsageMode.Designtime;
        }

        public Dialog(Form owner) : this()
        {
            Owner = owner;
        }

        protected override void OnShown(EventArgs e)
        {
            if (!IsInDesigner)
            {
                switch (StartPosition)
                {
                    case FormStartPosition.CenterParent:
                        CenterToParent();
                        break;
                    case FormStartPosition.CenterScreen:
                        CenterToScreen();
                        break;
                }
            }
            base.OnShown(e);
        }

        public virtual new void CenterToParent() => base.CenterToParent();
        public virtual new void CenterToScreen() => base.CenterToScreen();

        protected virtual bool ShouldSerializeBackColor() => !BackColor.Equals(DefaultBackColor);

        public override void ResetBackColor() => BackColor = DefaultBackColor;

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (e.KeyChar == '\x1b')
            {
                switch (OnEscKeyDown)
                {
                    case OnEscKeyDownAction.Close:
                        e.Handled = true;
                        Close();
                        return;
                    case OnEscKeyDownAction.Cancel:
                        if (CancelButton != null)
                        {
                            CancelButton.PerformClick();
                            return;
                        }
                        break;
                }
            }
            base.OnKeyPress(e);
        }
    }
}
