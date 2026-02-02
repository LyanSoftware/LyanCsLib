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
using RichTextBox_TextMode = Windows.Win32.UI.Controls.RichEdit.TEXTMODE;

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

    public static void SetDataSourceWithEnumDataAndDescriptionAndAdjustDropDownWidth<T>(this ComboBox cbx, Func<T, bool>? condition = null, Func<Utils.EnumDataWithDescription<T>, string>? descriptionPostProcessor = null, int extendWidth = 0) where T : Enum
    {
        var data = Utils.GetEnumDatasWithDescription<T>();
        if (condition != null)
            data = data.Where(d => condition(d.Value));
        if (descriptionPostProcessor == null)
        {
            var src = data.ToList();
            cbx.DataSource = src;
            cbx.DisplayMember = nameof(Utils.EnumDataWithDescription.Description);
            cbx.ValueMember = nameof(Utils.EnumDataWithDescription.Value);
            cbx.AdjustComboBoxDropDownWidth(src, x => x.Description, extendWidth);
        }
        else
        {
            var src = data.ToDictionary(d => d.Value, d => descriptionPostProcessor(d));
            cbx.DataSource = new BindingSource(src, null);
            cbx.DisplayMember = "Value";
            cbx.ValueMember = "Key";
            cbx.AdjustComboBoxDropDownWidth(src, x => x.Value, extendWidth);
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
    public static void AdjustComboBoxDropDownWidth(this ComboBox cbx, int extendWidth)
    => AdjustComboBoxDropDownWidth(cbx, null, (Func<object, string>?)null, extendWidth);

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

    public static T Invoke<T>(this Control control, Func<T> action)
    => (T)control.Invoke(action);

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

    public static void ErrBox(this IWin32Window? window, string msg, string title = "Error")
    {
        MessageBox.Show(window, msg, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    public static void ErrBox(this IWin32Window? window, Exception err, string title = "Error")
    {
        MessageBox.Show(window, (err.InnerException ?? err).Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    public static void WarnBox(this IWin32Window? window, string msg, string title = "Warning")
    {
        MessageBox.Show(window, msg, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    public static void InfoBox(this IWin32Window? window, string msg, string title = "Info")
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

    public static bool ClickClearSelection(this DataGridView editor, Point? clickScreenPos = null, bool endEdit = true)
    {
        if (editor.GetCellAtPoint(editor.PointToClient(clickScreenPos ?? Cursor.Position)) == null)
        {
            if (editor.IsCurrentCellInEditMode)
            {
                if (!endEdit)
                    return false;
                editor.EndEdit();
            }
            editor.ClearSelection();
            return true;
        }
        return false;
    }

    public static void ClickChangeCheckBoxOrClearSelection(this DataGridView editor, Point? clickScreenPos = null)
    {
        var pos = editor.PointToClient(clickScreenPos ?? Cursor.Position);
        var cell = editor.GetCellAtPoint(pos);
        var child = editor.GetChildAtPoint(pos);
        if (cell != null)
        {
            if (cell is DataGridViewCheckBoxCell cc && child == null)
            {
                if (editor.IsCurrentCellInEditMode)
                    editor.EndEdit();
                editor.BeginEdit(false);
                cc.Value = !(bool)cc.Value;
                editor.EndEdit();
                if (editor.SelectedCells.Count == 1)
                    editor.ClearSelection();
            }
            else if (editor.IsCurrentCellInEditMode)
                editor.EndEdit();
            else editor.BeginEdit(true);
        }
        else
        {
            if (editor.IsCurrentCellInEditMode)
                editor.EndEdit();
            editor.ClearSelection();
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int EditWordBreakProcDelegate(IntPtr text, int pos_in_text, int bCharSet, int action);

    public static void SetDefaultWordbreak(IntPtr handle, ushort width, ushort height, bool enable = false)
    {
        EditWordBreakProcDelegate f = (_, _, _, _) => 0;
        var cbFunc = enable ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(f);
        PInvoke.SendMessage(new(handle), PInvoke.EM_SETWORDBREAKPROC, 0, cbFunc);
        PInvoke.SendMessage(new(handle), PInvoke.WM_SIZE, 0, new IntPtr((width << 16) | height));
    }
    public static void SetDefaultWordbreak(this RichTextBox box, bool enable = false)
    => SetDefaultWordbreak(box.Handle, (ushort)box.Width, (ushort)box.Height, enable);

    [Flags]
    public enum RichTextMode
    {
        PlainText = RichTextBox_TextMode.TM_PLAINTEXT,
        RichText = RichTextBox_TextMode.TM_RICHTEXT,
        SingleLevelUndo = RichTextBox_TextMode.TM_SINGLECODEPAGE,
        MultiLevelUndo = RichTextBox_TextMode.TM_MULTILEVELUNDO,
        SingleCodePage = RichTextBox_TextMode.TM_SINGLECODEPAGE,
        MultiCodePage = RichTextBox_TextMode.TM_MULTICODEPAGE,
    }

    public static void SetTextMode(this RichTextBox box, RichTextMode mode)
    => PInvoke.SendMessage(new(box.Handle), PInvoke.EM_SETTEXTMODE, (uint)mode, IntPtr.Zero);
}
