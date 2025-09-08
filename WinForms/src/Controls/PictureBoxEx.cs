using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Text;
using System.Drawing.Drawing2D;

namespace Lytec.WinForms
{
    public class PictureBoxEx : PictureBox
    {
        [Browsable(true)]
        [DefaultValue(typeof(InterpolationMode), nameof(InterpolationMode.Default))]
        public InterpolationMode InterpolationMode { get; set; } = InterpolationMode.Default;

        [Browsable(true)]
        [DefaultValue(typeof(PixelOffsetMode), nameof(PixelOffsetMode.Default))]
        public PixelOffsetMode PixelOffsetMode { get; set; } = PixelOffsetMode.Default;

        protected override void OnPaint(PaintEventArgs pe)
        {
            pe.Graphics.InterpolationMode = InterpolationMode;
            pe.Graphics.PixelOffsetMode = PixelOffsetMode;
            base.OnPaint(pe);
        }
    }
}
