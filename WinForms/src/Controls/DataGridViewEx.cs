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
    public class DataGridViewEx : DataGridView
    {
        public delegate Image ColumnEventHandler(int columnIndex);

        public event ColumnEventHandler? GetColumnHeaderImage;

        protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
        {
            base.OnCellPainting(e);
            if (!e.Handled && e.RowIndex == -1 && GetColumnHeaderImage?.Invoke(e.ColumnIndex) is Image img)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All);
                var pos = e.CellBounds.Location;
                var padding = Columns[e.ColumnIndex].DefaultCellStyle?.Padding ?? new();
                var w = e.CellBounds.Width - padding.Horizontal;
                var h = e.CellBounds.Height - padding.Vertical;
                if (w > h)
                    pos.X += (w - h) / 2 + padding.Left;
                else pos.Y += (h - w) / 2 + padding.Top;
                pos.X += 1; // 显示效果莫名的往左偏，手动向右矫正一点
                var size = Math.Min(w, h);
                e.Graphics.DrawImage(img, new Rectangle(pos, new(size, size)), new Rectangle(new(), img.Size), GraphicsUnit.Pixel);
                e.Handled = true;
            }
        }
    }
}
