using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Lytec.WinForms;

public class CenterWinFormMsgBox : IDisposable
{
    private int mTries = 0;
    private readonly Form mOwner;

    public CenterWinFormMsgBox(Form owner)
    {
        mOwner = owner;
        // 开始异步查找 MessageBox 窗口
        owner.BeginInvoke(new MethodInvoker(findDialog));
    }

    private void findDialog()
    {
        if (mTries < 0) return;
        // 枚举当前线程的所有窗口，查找 MessageBox
        EnumThreadWndProc callback = new EnumThreadWndProc(checkWindow);
        if (EnumThreadWindows(GetCurrentThreadId(), callback, IntPtr.Zero))
        {
            if (++mTries < 10) // 最多尝试10次
                mOwner.BeginInvoke(new MethodInvoker(findDialog));
        }
    }

    private bool checkWindow(IntPtr hWnd, IntPtr lp)
    {
        // 检查窗口类名是否为 MessageBox 的类名 "#32770"
        StringBuilder sb = new StringBuilder(260);
        GetClassName(hWnd, sb, sb.Capacity);
        if (sb.ToString() != "#32770")
            return true; // 不是目标窗口，继续枚举

        // 计算并移动窗口到父窗体中央
        Rectangle frmRect = new Rectangle(mOwner.Location, mOwner.Size);
        RECT dlgRect;
        GetWindowRect(hWnd, out dlgRect);
        MoveWindow(hWnd,
            frmRect.Left + (frmRect.Width - (dlgRect.Right - dlgRect.Left)) / 2,
            frmRect.Top + (frmRect.Height - (dlgRect.Bottom - dlgRect.Top)) / 2,
            dlgRect.Right - dlgRect.Left,
            dlgRect.Bottom - dlgRect.Top,
            true);
        return false; // 找到并处理完毕，停止枚举
    }

    public void Dispose()
    {
        mTries = -1; // 停止查找
    }

    // P/Invoke 声明
    private delegate bool EnumThreadWndProc(IntPtr hWnd, IntPtr lp);
    [DllImport("user32.dll")]
    private static extern bool EnumThreadWindows(int tid, EnumThreadWndProc callback, IntPtr lp);
    [DllImport("kernel32.dll")]
    private static extern int GetCurrentThreadId();
    [DllImport("user32.dll")]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder buffer, int buflen);
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT rc);
    [DllImport("user32.dll")]
    private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);

    private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
}
