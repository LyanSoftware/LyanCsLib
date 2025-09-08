using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;
using Lytec.Common;
using Timer = System.Windows.Forms.Timer;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

namespace Lytec.WinForms;

public static partial class WinFormUtils
{
    private static Lazy<Icon?> _defaultFormIcon
    = new(() => typeof(Form)
                    .GetProperty("DefaultIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                    .GetValue(null, null)
                    is Icon icon ? icon : null);
    public static Icon? DefaultFormIcon => _defaultFormIcon.Value;

    public static void Deconstruct(this Size size, out int Width, out int Height)
    {
        Width = size.Width;
        Height = size.Height;
    }

    public static void Restart(this Timer timer, bool triggerNow = false)
    {
        timer.Stop();
        if (triggerNow)
            timer.TriggerEvent("onTimer");
        timer.Start();
    }

    public static void PaintDataGridViewCheckBoxCell(this DataGridView view, DataGridViewCellPaintingEventArgs e, int checkBoxSize = 16)
    {
        if (e.RowIndex >= 0 && view.Columns[e.ColumnIndex] is DataGridViewCheckBoxColumn chkCol)
        {
            e.PaintBackground(e.CellBounds, true);
            var state = ButtonState.Normal;
            if (view[e.ColumnIndex, e.RowIndex].ReadOnly)
                state |= ButtonState.Inactive;
            if ((bool)e.FormattedValue)
                state |= ButtonState.Checked;
            if (chkCol.FlatStyle < FlatStyle.Standard)
                state |= ButtonState.Flat;

            ControlPaint.DrawCheckBox(e.Graphics,
                e.CellBounds.X + (e.CellBounds.Width / 2) - (checkBoxSize / 2),
                e.CellBounds.Y + (e.CellBounds.Height / 2) - (checkBoxSize / 2),
                checkBoxSize,
                checkBoxSize,
                state);
            e.Handled = true;
        }
    }

    public static void SetDropDownWidthToComboBoxWidth(this ComboBox cbx)
    {
        cbx.DropDown += (sender, e) => cbx.DropDownWidth = cbx.Width;
    }

    public static void SetDataSourceWithEnumDataAndDescription<T>(this ComboBox cbx, Func<T, bool>? condition = null, Func<Utils.EnumDataWithDescription<T>, string>? descriptionPostProcessor = null) where T : Enum
    {
        var data = Utils.GetEnumDatasWithDescription<T>();
        if (condition != null)
            data = data.Where(d => condition(d.Value));
        if (descriptionPostProcessor == null)
        {
            cbx.DataSource = data.ToList();
            cbx.DisplayMember = nameof(Utils.EnumDataWithDescription.Description);
            cbx.ValueMember = nameof(Utils.EnumDataWithDescription.Value);
        }
        else
        {
            cbx.DataSource = new BindingSource(data.ToDictionary(d => d.Value, d => descriptionPostProcessor(d)), null);
            cbx.DisplayMember = "Value";
            cbx.ValueMember = "Key";
        }
    }

    public static void ScrollToEnd(this TextBox tbx)
    {
        tbx.SelectionLength = 0;
        tbx.SelectionStart = tbx.TextLength;
        tbx.ScrollToCaret();
    }

    public static void ShowMe(this Form form)
    {
        form.Show();
        if (form.WindowState == FormWindowState.Minimized)
            form.WindowState = FormWindowState.Normal;
        form.Activate();
        form.BringToFront();
    }

    public static void ShowMe(this Form form, IWin32Window owner)
    {
        form.Show(owner);
        if (form.WindowState == FormWindowState.Minimized)
            form.WindowState = FormWindowState.Normal;
        form.Activate();
        form.BringToFront();
    }

    public static DataGridViewCell? GetCellAtPoint(this DataGridView view, Point clientPoint)
    {
        var hit = view.HitTest(clientPoint.X, clientPoint.Y);
        var isHitCell = hit.Type == DataGridViewHitTestType.Cell && hit.ColumnIndex >= 0 && hit.RowIndex >= 0;
        return isHitCell ? view[hit.ColumnIndex, hit.RowIndex] : null;
    }

    public static void SetCommitEditOnCheckBoxColumn(this DataGridView view)
    => view.CurrentCellDirtyStateChanged += (sender, e) =>
    {
        if (sender is DataGridView dgv && dgv.CurrentCell is DataGridViewCheckBoxCell)
            dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
    };

    public static void SetToExecutableFileIcon(this Form form) => form.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

    /// <summary>
    /// 根据内容自动计算下拉框宽度
    /// </summary>
    /// <param name="cbx"></param>
    /// <param name="args"></param>
    public static void AdjustComboBoxDropDownWidth(this ComboBox cbx, Func<object, string>? toString = default, int extendWidth = 0)
    => AdjustComboBoxDropDownWidth(cbx, null, toString, extendWidth);

    /// <summary>
    /// 根据内容自动计算下拉框宽度
    /// </summary>
    /// <param name="cbx"></param>
    /// <param name="args"></param>
    public static void AdjustComboBoxDropDownWidth<T>(this ComboBox cbx, Func<T, string>? toString = default, int extendWidth = 0)
    => AdjustComboBoxDropDownWidth(cbx, null, toString, extendWidth);

    /// <summary>
    /// 根据内容自动计算下拉框宽度
    /// </summary>
    /// <param name="cbx"></param>
    /// <param name="args"></param>
    public static void AdjustComboBoxDropDownWidth<T>(this ComboBox cbx, IEnumerable<T>? dataSource, Func<T, string>? toString, int extendWidth = 0)
    {
        dataSource = null; // 这个参数只是为了自动取泛型类型
        if (toString == default)
            toString = obj => obj!.ToString();
        cbx.DropDownWidth = (
            from T item
            in cbx.Items
            select TextRenderer.MeasureText(toString(item), cbx.Font).Width + extendWidth
            ).Max();
    }

    /// <summary>
    /// 根据内容自动计算宽度
    /// </summary>
    /// <param name="cbx"></param>
    /// <param name="args"></param>
    public static void AdjustComboBoxWidth(this ComboBox cbx, int extendWidth = 0)
    => cbx.Width = TextRenderer.MeasureText(cbx.Text + ((cbx.DropDownStyle != ComboBoxStyle.Simple) ? "  " : ""), cbx.Font).Width + extendWidth;

    /// <summary>
    /// 根据内容自动计算宽度
    /// </summary>
    /// <param name="cbx"></param>
    /// <param name="args"></param>
    public static void AdjustComboBoxWidth(this ComboBox cbx, string extendStr, int extendWidth = 0)
    => cbx.Width = TextRenderer.MeasureText(cbx.Text + ((cbx.DropDownStyle != ComboBoxStyle.Simple) ? "  " : "") + extendStr, cbx.Font).Width + extendWidth;

    /// <summary>
    /// 根据内容自动计算宽度
    /// </summary>
    /// <param name="cbx"></param>
    /// <param name="args"></param>
    public static void AdjustComboBoxWidth<T>(
        this ComboBox cbx,
#pragma warning disable IDE0060 // 删除未使用的参数
        IEnumerable<T>? dataSource,
#pragma warning restore IDE0060 // 删除未使用的参数
        Func<T, string>? toString = default,
        int extendWidth = 0
        )
    {
#pragma warning disable IDE0059 // 不需要赋值
        dataSource = null; // 这个参数只是为了自动取泛型类型
#pragma warning restore IDE0059 // 不需要赋值
        if (toString == default)
            toString = obj => obj!.ToString();
        cbx.Width = TextRenderer.MeasureText(toString((T)cbx.SelectedValue) + ((cbx.DropDownStyle != ComboBoxStyle.Simple) ? "  " : ""), cbx.Font).Width + extendWidth;
    }

    /// <summary>
    /// 取消选择所有单元格
    /// </summary>
    /// <param name="table"></param>
    /// <param name="cancelEdit">是否撤销当前单元格的编辑</param>
    /// <returns></returns>
    public static bool ClearSelection(this DataGridView table, bool cancelEdit = false)
    {
        var ret = cancelEdit ? table.CancelEdit() : table.EndEdit();
        if (ret)
        {
            table.ClearSelection();
            table.CurrentCell = null;
        }
        return ret;
    }

    /// <summary>
    /// 计算窗体实际边框大小
    /// </summary>
    /// <param name="form"></param>
    /// <returns></returns>
    public static bool GetWindowBorderWeight(this Form form, out Rectangle Rect)
    {
        var style = form.FormBorderStyle;
        unsafe
        {
            RECT rect = default;
            Rect = default;
            if (PInvoke.DwmGetWindowAttribute(new(form.Handle), DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, &rect, (uint)sizeof(RECT))
                == HRESULT.S_OK)
            {
                var point = form.PointToScreen(new Point());
                var size = form.Size;
                form.FormBorderStyle = FormBorderStyle.None;
                var lt = new Point(point.X - rect.left, point.Y - rect.top);
                var rb = new Size(size.Width - form.Width - lt.X, size.Height - form.Height - lt.Y);
                form.FormBorderStyle = style;
                Rect = new(lt, rb);
                return true;
            }
        }
        return false;
    }

    public static void HideImageMargin(this ToolStrip menu, int recursiveDepth = 0)
    {
        if (menu is ToolStripDropDownMenu ddMenu)
            ddMenu.ShowImageMargin = false;
        foreach (ToolStripItem item in menu.Items)
            HideImageMargin(item, recursiveDepth - 1);
    }

    public static void HideImageMargin(this MenuStrip menu, int recursiveDepth = 0)
    {
        recursiveDepth++;
        foreach (ToolStripItem tsItem in menu.Items)
            HideImageMargin(tsItem, recursiveDepth - 1);
    }

    public static void HideImageMargin(this ToolStripItem item, int recursiveDepth = 0)
    {
        if (recursiveDepth < 0)
            return;
        if (item is ToolStripMenuItem tsmItem && tsmItem.DropDown is ToolStripDropDownMenu ddMenu)
        {
            ddMenu.ShowImageMargin = false;
            foreach (ToolStripItem tsmItem1 in ddMenu.Items)
                HideImageMargin(tsmItem1, recursiveDepth - 1);
        }
    }

    public static object UIAction<T>(this T control, Action action, bool lazy = false) where T : Control
    => lazy ? control.BeginInvoke(action) : control.Invoke((Delegate)action);

    public static TResult Invoke<T, TResult>(this T control, Func<TResult> action) where T : Control
    => (TResult)control.Invoke(action);

    protected class AsyncResult : IAsyncResult
    {
        public static readonly AsyncResult Success = new AsyncResult()
        {
            IsCompleted = true,
            AsyncWaitHandle = null,
            AsyncState = null,
            CompletedSynchronously = true,
        };

        public bool IsCompleted { get; set; }

        public WaitHandle? AsyncWaitHandle { get; set; }

        public object? AsyncState { get; set; }

        public bool CompletedSynchronously { get; set; }
    }

    public static IAsyncResult Invoke<T>(this T control, Action action, bool lazy = false) where T : Control
    {
        if (lazy)
            return control.BeginInvoke(action);
        control.Invoke(action);
        return AsyncResult.Success;
    }

    public static Task InvokeAsync<T>(this T control, Action action) where T : Control
    => Task.Run(() => control.Invoke(action));

    public static Task<Result> InvokeAsync<T, Result>(this T control, Func<Result> action) where T : Control
    => Task.Run(() => (Result)control.Invoke(action));

    public static Task InvokeAsync<T>(this T control, Func<Task> action) where T : Control
    => Task.Run(() => control.Invoke(action));

    public static Task<Result> InvokeAsync<T, Result>(this T control, Func<Task<Result>> action) where T : Control
    => Task.Run(() => (Result)control.Invoke(() =>
    {
        var task = action();
        task.ConfigureAwait(false);
        return task.Result;
    }));

    public static void ErrBox(this IWin32Window window, string msg, string title = "Error")
    {
        MessageBox.Show(window, msg, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    public static void ErrBox(this IWin32Window window, Exception err, string title = "Error")
    {
        MessageBox.Show(window, (err.InnerException ?? err).Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    public static void WarnBox(this IWin32Window window, string msg, string title = "Warning")
    {
        MessageBox.Show(window, msg, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    public static void InfoBox(this IWin32Window window, string msg, string title = "Info")
    {
        MessageBox.Show(window, msg, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public static void SafeSetValue(this NumericUpDown input, decimal value)
    => input.Value = Math.Max(Math.Min(input.Maximum, value), input.Minimum);

    public static bool IsAdministrator = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

    public static void ScrollToBottom(this ListBox lb)
    {
        lb.ClearSelected();
        lb.SelectedIndex = lb.Items.Count - 1;
    }
}
