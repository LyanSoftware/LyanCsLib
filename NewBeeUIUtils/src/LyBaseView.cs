using Avalonia.Controls;

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace NewBeeUI;
#pragma warning restore IDE0130 // 命名空间与文件夹结构不匹配

public abstract class LyBaseView : BaseView
{
    public static DockPanel DockPanel(params Control[] children) => new DockPanel().Children(children);
    public static DockPanel DockPanel(params Control[]?[]? arrs) => new DockPanel().Children(arrs);
}
