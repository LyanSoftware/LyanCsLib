using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Lytec.Wpf;

/*

实现css的 :first-child :last-child :nth-child

在需要使用的容器上添加 local:ItemsControlExtensions.AddItemIndexProperties="True"

扩展属性：
IsFirstItem/IsLastItem/ItemIndex/IsEvenItem/IsOddItem

:first-child
:last-child
<!-- 为 ItemContainer（如 ListBoxItem）定义样式 -->
<Style TargetType="ListBoxItem">
    <!-- 默认背景 -->
    <Setter Property="Background" Value="White"/>
    <Style.Triggers>
        <!-- 第一项 -->
        <DataTrigger Binding="{Binding Path=(local:ItemsControlExtensions.IsFirstItem), RelativeSource={RelativeSource Self}}" Value="True">
            <Setter Property="Background" Value="LightGreen"/>
            <Setter Property="FontWeight" Value="Bold"/>
        </DataTrigger>
        <!-- 最后一项 -->
        <DataTrigger Binding="{Binding Path=(local:ItemsControlExtensions.IsLastItem), RelativeSource={RelativeSource Self}}" Value="True">
            <Setter Property="Background" Value="LightCoral"/>
            <Setter Property="FontStyle" Value="Italic"/>
        </DataTrigger>
    </Style.Triggers>
</Style>

:nth-child(odd)
:nth-child(even)
<Style TargetType="ListBoxItem">
    <Setter Property="Background" Value="White"/>
    <Style.Triggers>
        <!-- 奇数行（索引 1,3,5...） -->
        <DataTrigger Binding="{Binding Path=(local:ItemsControlExtensions.IsOdd), RelativeSource={RelativeSource Self}}" Value="True">
            <Setter Property="Background" Value="LightYellow"/>
        </DataTrigger>
        <!-- 偶数行（索引 0,2,4...） -->
        <DataTrigger Binding="{Binding Path=(local:ItemsControlExtensions.IsEven), RelativeSource={RelativeSource Self}}" Value="True">
            <Setter Property="Background" Value="LightBlue"/>
        </DataTrigger>
    </Style.Triggers>
</Style>

:nth-child(n)
<local:IndexMatchConverter x:Key="IndexMatchConv"/>
<Style TargetType="ListBoxItem">
    <Style.Triggers>
        <DataTrigger Value="True">
            <DataTrigger.Binding>
                <MultiBinding Converter="{StaticResource IndexMatchConv}">
                    <Binding Path="(local:ItemsControlExtensions.Index)" RelativeSource="{RelativeSource Self}"/>
                    <Binding Source="2"/> <!-- 索引2 = 第3个元素 -->
                </MultiBinding>
            </DataTrigger.Binding>
            <Setter Property="Background" Value="Orange"/>
            <Setter Property="FontWeight" Value="Bold"/>
        </DataTrigger>
    </Style.Triggers>
</Style>

:nth-child(an)
<DataTrigger Value="True">
    <DataTrigger.Binding>
        <MultiBinding Converter="{StaticResource ModuloMatchConv}">
            <Binding Path="(local:ItemsControlExtensions.Index)" RelativeSource="{RelativeSource Self}"/>
            <Binding Source="3"/>
        </MultiBinding>
    </DataTrigger.Binding>
    <Setter Property="Background" Value="LightGreen"/>
</DataTrigger>

*/

public static class ItemsControlExtensions
{
    // 启用自动检测的附加属性（附着到 ItemsControl）
    public static readonly DependencyProperty AddItemIndexPropertiesProperty =
        DependencyProperty.RegisterAttached("AddItemIndexProperties", typeof(bool), typeof(ItemsControlExtensions),
            new PropertyMetadata(false, OnAddItemIndexPropertiesChanged));

    public static bool GetAddItemIndexProperties(DependencyObject obj) => (bool)obj.GetValue(AddItemIndexPropertiesProperty);
    public static void SetAddItemIndexProperties(DependencyObject obj, bool value) => obj.SetValue(AddItemIndexPropertiesProperty, value);

    private static void OnAddItemIndexPropertiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ItemsControl itemsControl) return;

        void OnGeneratorStatusChanged(object sender, EventArgs e)
        {
            if (sender is ItemContainerGenerator g && g.Status == GeneratorStatus.ContainersGenerated)
                UpdateItemIndexStatus(itemsControl);
        }
        if ((bool)e.NewValue)
        {
            itemsControl.ItemContainerGenerator.StatusChanged += OnGeneratorStatusChanged;
            // 立即刷新一次
            UpdateItemIndexStatus(itemsControl);
            itemsControl.ItemContainerGenerator.ItemsChanged += OnGeneratorStatusChanged;
        }
        else
        {
            itemsControl.ItemContainerGenerator.StatusChanged -= OnGeneratorStatusChanged;
            itemsControl.ItemContainerGenerator.ItemsChanged -= OnGeneratorStatusChanged;
        }
    }

    // 附加属性：ItemIndex
    public static readonly DependencyProperty ItemIndexProperty =
        DependencyProperty.RegisterAttached("ItemIndex", typeof(int), typeof(ItemsControlExtensions),
            new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.Inherits));

    public static int GetItemIndex(DependencyObject obj) => (int)obj.GetValue(ItemIndexProperty);
    public static void SetItemIndex(DependencyObject obj, int value) => obj.SetValue(ItemIndexProperty, value);

    // 附加属性：IsFirstItem
    public static readonly DependencyProperty IsFirstItemProperty =
        DependencyProperty.RegisterAttached("IsFirstItem", typeof(bool), typeof(ItemsControlExtensions),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    public static bool GetIsFirstItem(DependencyObject obj) => (bool)obj.GetValue(IsFirstItemProperty);
    public static void SetIsFirstItem(DependencyObject obj, bool value) => obj.SetValue(IsFirstItemProperty, value);

    // 附加属性：IsLastItem
    public static readonly DependencyProperty IsLastItemProperty =
        DependencyProperty.RegisterAttached("IsLastItem", typeof(bool), typeof(ItemsControlExtensions),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    public static bool GetIsLastItem(DependencyObject obj) => (bool)obj.GetValue(IsLastItemProperty);
    public static void SetIsLastItem(DependencyObject obj, bool value) => obj.SetValue(IsLastItemProperty, value);

    // 附加属性：IsEvenItem
    public static readonly DependencyProperty IsEvenItemProperty =
        DependencyProperty.RegisterAttached("IsEvenItem", typeof(bool), typeof(ItemsControlExtensions),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    public static bool GetIsEvenItem(DependencyObject obj) => (bool)obj.GetValue(IsEvenItemProperty);
    public static void SetIsEvenItem(DependencyObject obj, bool value) => obj.SetValue(IsEvenItemProperty, value);

    // 附加属性：IsOddItem
    public static readonly DependencyProperty IsOddItemProperty =
        DependencyProperty.RegisterAttached("IsOddItem", typeof(bool), typeof(ItemsControlExtensions),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    public static bool GetIsOddItem(DependencyObject obj) => (bool)obj.GetValue(IsOddItemProperty);
    public static void SetIsOddItem(DependencyObject obj, bool value) => obj.SetValue(IsOddItemProperty, value);

    // 修改 UpdateItemIndexStatus 方法，同时设置 Index, IsEven, IsOdd
    private static void UpdateItemIndexStatus(ItemsControl itemsControl)
    {
        if (itemsControl == null) return;

        var count = itemsControl.Items.Count;
        for (int i = 0; i < count; i++)
        {
            var container = itemsControl.ItemContainerGenerator.ContainerFromIndex(i);
            if (container != null)
            {
                SetIsFirstItem(container, i == 0);
                SetIsLastItem(container, i == count - 1);
                SetItemIndex(container, i);
                SetIsEvenItem(container, i % 2 == 0);
                SetIsOddItem(container, i % 2 == 1);
            }
        }
    }
}
