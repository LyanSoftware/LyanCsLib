using System.Windows;
using System.Windows.Controls;
using System.Collections.Specialized;
using System;

namespace Lytec.Wpf;

/*

实现css的 :first-child :last-child :nth-child

在需要使用的容器上添加 local:PanelExtensions.AddChildIndexProperties="True"

扩展属性：
IsFirstChild/IsLastChild/ChildIndex/IsEvenChild/IsOddChild

:first-child
:last-child
<!-- 为 Panel（如 StackPanel）定义样式 -->
<Style TargetType="Button">
    <Style.Triggers>
        <DataTrigger Binding="{Binding Path=(local:PanelExtensions.IsFirstChild), RelativeSource={RelativeSource Self}}" Value="True">
            <Setter Property="Background" Value="LightGreen"/>
        </DataTrigger>
        <DataTrigger Binding="{Binding Path=(local:PanelExtensions.IsLastChild), RelativeSource={RelativeSource Self}}" Value="True">
            <Setter Property="Background" Value="LightCoral"/>
        </DataTrigger>
    </Style.Triggers>
</Style>

:nth-child(odd)
:nth-child(even)
:nth-child(n)
:nth-child(an)
参照ItemsControlExtensions实现

 */

public static class PanelExtensions
{
    // 附加属性：IsFirstChild, IsLastChild, ChildIndex, IsEvenChild, IsOddChild
    public static readonly DependencyProperty IsFirstChildProperty =
        DependencyProperty.RegisterAttached("IsFirstChild", typeof(bool), typeof(PanelExtensions));
    public static bool GetIsFirstChild(DependencyObject obj) => (bool)obj.GetValue(IsFirstChildProperty);
    public static void SetIsFirstChild(DependencyObject obj, bool value) => obj.SetValue(IsFirstChildProperty, value);

    public static readonly DependencyProperty IsLastChildProperty =
        DependencyProperty.RegisterAttached("IsLastChild", typeof(bool), typeof(PanelExtensions));
    public static bool GetIsLastChild(DependencyObject obj) => (bool)obj.GetValue(IsLastChildProperty);
    public static void SetIsLastChild(DependencyObject obj, bool value) => obj.SetValue(IsLastChildProperty, value);

    public static readonly DependencyProperty ChildIndexProperty =
        DependencyProperty.RegisterAttached("ChildIndex", typeof(int), typeof(PanelExtensions));
    public static int GetChildIndex(DependencyObject obj) => (int)obj.GetValue(ChildIndexProperty);
    public static void SetChildIndex(DependencyObject obj, int value) => obj.SetValue(ChildIndexProperty, value);

    public static readonly DependencyProperty IsEvenChildProperty =
        DependencyProperty.RegisterAttached("IsEvenChild", typeof(bool), typeof(PanelExtensions));
    public static bool GetIsEvenChild(DependencyObject obj) => (bool)obj.GetValue(IsEvenChildProperty);
    public static void SetIsEvenChild(DependencyObject obj, bool value) => obj.SetValue(IsEvenChildProperty, value);

    public static readonly DependencyProperty IsOddChildProperty =
        DependencyProperty.RegisterAttached("IsOddChild", typeof(bool), typeof(PanelExtensions));
    public static bool GetIsOddChild(DependencyObject obj) => (bool)obj.GetValue(IsOddChildProperty);
    public static void SetIsOddChild(DependencyObject obj, bool value) => obj.SetValue(IsOddChildProperty, value);

    // 启用自动检测的附加属性（附着到 Panel）
    public static readonly DependencyProperty AddChildIndexPropertiesProperty =
        DependencyProperty.RegisterAttached("AddChildIndexProperties", typeof(bool), typeof(PanelExtensions),
            new PropertyMetadata(false, OnAddChildIndexPropertiesChanged));

    public static bool GetAddChildIndexProperties(DependencyObject obj) => (bool)obj.GetValue(AddChildIndexPropertiesProperty);
    public static void SetAddChildIndexProperties(DependencyObject obj, bool value) => obj.SetValue(AddChildIndexPropertiesProperty, value);

    private static void OnAddChildIndexPropertiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Panel panel) return;

        if ((bool)e.NewValue)
        {
            // 监听子元素集合变化
            if (panel.Children is INotifyCollectionChanged observable)
                observable.CollectionChanged += (s, args) => UpdateChildrenStatus(panel);
            panel.LayoutUpdated += OnLayoutUpdated; // 用于初始化及布局变化时刷新
            UpdateChildrenStatus(panel);
        }
        else
        {
            panel.LayoutUpdated -= OnLayoutUpdated;
        }
    }

    private static void OnLayoutUpdated(object sender, EventArgs e)
    {
        if (sender is Panel panel)
            UpdateChildrenStatus(panel);
    }

    private static void UpdateChildrenStatus(Panel panel)
    {
        var children = panel.Children;
        int count = children.Count;
        for (int i = 0; i < count; i++)
        {
            var child = children[i];
            SetIsFirstChild(child, i == 0);
            SetIsLastChild(child, i == count - 1);
            SetChildIndex(child, i);
            SetIsEvenChild(child, i % 2 == 0);
            SetIsOddChild(child, i % 2 == 1);
        }
    }
}
