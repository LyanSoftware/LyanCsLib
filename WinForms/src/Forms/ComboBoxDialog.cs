using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lytec.WinForms
{
    public partial class ComboBoxDialog : Dialog
    {
        public string Title { get => Text; set => Text = value; }
        public string TipText { get => TipLabel.Text; set => TipLabel.Text = value; }
        public string OkButtonText { get => ButtonOk.Text; set => ButtonOk.Text = value; }
        public string CancelButtonText { get => ButtonCancel.Text; set => ButtonCancel.Text = value; }

        public ComboBoxDialog()
        {
            InitializeComponent();
        }
    }
}
