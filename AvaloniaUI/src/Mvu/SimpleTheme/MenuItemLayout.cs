using System;
using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Converters;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Styling;
using Path = Avalonia.Controls.Shapes.Path;

namespace Lytec.AvaloniaUI.Mvu.SimpleTheme;

public static class MenuItemLayout
{
    /// <summary>
    /// 是否保留此 MenuItem 的直接子菜单左侧的 Icon / Check / Radio 列。
    ///
    /// </summary>
    public static readonly AttachedProperty<bool> ShowIconColumnProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>(
            "ShowIconColumn",
            typeof(MenuItemLayout),
            defaultValue: true);

    public static bool GetShowIconColumn(Control control)
        => control.GetValue(ShowIconColumnProperty);

    public static void SetShowIconColumn(Control control, bool value)
        => control.SetValue(ShowIconColumnProperty, value);

    /// <summary>
    /// 当前 MenuItem 的直接子菜单全部没有子菜单时，箭头列保留的最小宽度。
    ///
    /// </summary>
    public static readonly AttachedProperty<double> EmptySubmenuArrowColumnWidthProperty =
        AvaloniaProperty.RegisterAttached<Control, double>(
            "EmptySubmenuArrowColumnWidth",
            typeof(MenuItemLayout),
            defaultValue: 8,
            validate: static value => double.IsFinite(value) && value >= 0);

    public static double GetEmptySubmenuArrowColumnWidth(Control control)
        => control.GetValue(EmptySubmenuArrowColumnWidthProperty);

    public static void SetEmptySubmenuArrowColumnWidth(Control control, double value)
        => control.SetValue(EmptySubmenuArrowColumnWidthProperty, value);

    /// <summary>
    /// InputGesture 文本使用的独立主题。
    ///
    /// </summary>
    public static readonly AttachedProperty<ControlTheme?> InputGestureTextThemeProperty =
        AvaloniaProperty.RegisterAttached<Control, ControlTheme?>(
            "InputGestureTextTheme",
            typeof(MenuItemLayout));

    public static ControlTheme? GetInputGestureTextTheme(Control control)
        => control.GetValue(InputGestureTextThemeProperty);

    public static void SetInputGestureTextTheme(
        Control control,
        ControlTheme? value)
        => control.SetValue(InputGestureTextThemeProperty, value);

    /// <summary>
    /// 添加到 Application.Styles。
    ///
    /// 只替换下拉 MenuItem 的 Template，
    /// 不影响 MenuBar 顶级 MenuItem。
    /// </summary>
    public static readonly Style Style =
        new(x => x
            .Is<MenuItem>()
            .Descendant()
            .Is<MenuItem>()
            .Not(y => y.Class(":separator")))
        {
            Setters =
            {
                new Setter(
                    TemplatedControl.TemplateProperty,
                    new FuncControlTemplate<MenuItem>(BuildTemplate))
            }
        };

    private static Border BuildTemplate(MenuItem item, INameScope scope)
    {
        //
        // SimpleTheme 原版：
        //
        //   20 | 5 | * | Auto | 20
        //    ↑   ↑
        //    Icon  Spacer
        //
        // 这里结构完全保留，只把前两列改成：
        //
        // ShowIconColumn
        //     true  -> 20, 5
        //     false ->  0, 0
        //

        // 此模板属于子 MenuItem，但 ShowIconColumn 配置的是父 MenuItem
        // 弹出的整层子菜单。每个兄弟子项都读取同一个父项值，因此整列
        // 会一起显示或隐藏，同时不会影响父项的同级菜单。
        //
        // ColumnDefinition 不在模板的可视树中，先由 LayoutGrid 接收父项的值，
        // 再让列定义绑定到这个代理值。
        var grid = new LayoutGrid();

        if (item.Parent is MenuItem owner)
        {
            grid.Bind(
                LayoutGrid.ShowIconColumnProperty,
                owner.GetObservable(ShowIconColumnProperty));
        }
        else
        {
            grid.ShowIconColumn = true;
        }

        var iconColumn = new ColumnDefinition();

        iconColumn[!ColumnDefinition.WidthProperty] =
            new Binding(nameof(LayoutGrid.ShowIconColumn))
            {
                Source = grid,
                Converter = BoolToGridLengthConverter.Instance,
                ConverterParameter = new GridLength(20),
            };

        var iconSpacerColumn = new ColumnDefinition();

        iconSpacerColumn[!ColumnDefinition.WidthProperty] =
            new Binding(nameof(LayoutGrid.ShowIconColumn))
            {
                Source = grid,
                Converter = BoolToGridLengthConverter.Instance,
                ConverterParameter = new GridLength(5),
            };

        var headerColumn = new ColumnDefinition
        {
            Width = GridLength.Star
        };

        var inputGestureColumn = new ColumnDefinition
        {
            Width = GridLength.Auto,
            SharedSizeGroup = "MenuItemIGT"
        };

        var arrowColumn = new ColumnDefinition
        {
            Width = GridLength.Auto,
            SharedSizeGroup = "MenuItemArrow"
        };

        if (item.Parent is Control parentMenuItem)
        {
            arrowColumn.Bind(
                ColumnDefinition.MinWidthProperty,
                parentMenuItem.GetObservable(EmptySubmenuArrowColumnWidthProperty));
        }
        else
        {
            arrowColumn.MinWidth = 4;
        }

        grid.ColumnDefinitions.Add(iconColumn);
        grid.ColumnDefinitions.Add(iconSpacerColumn);
        grid.ColumnDefinitions.Add(headerColumn);
        grid.ColumnDefinitions.Add(inputGestureColumn);
        grid.ColumnDefinitions.Add(arrowColumn);

        //
        // Toggle / Radio check mark presenter
        //

        var toggleIconPresenter = Register(
            new ContentControl
            {
                Name = "PART_ToggleIconPresenter",
                IsVisible = false,
                Margin = new Thickness(3),
                Width = 16,
                Height = 16,
            },
            scope);

        Grid.SetColumn(toggleIconPresenter, 0);

        //
        // Icon presenter
        //

        var iconPresenter = Register(
            new ContentControl
            {
                Name = "PART_IconPresenter",
                Width = 16,
                Height = 16,
                Margin = new Thickness(3),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,

                [!ContentControl.ContentProperty] =
                    new TemplateBinding(MenuItem.IconProperty),
            },
            scope);

        Grid.SetColumn(iconPresenter, 0);

        //
        // Header
        //

        var headerPresenter = Register(
            new ContentPresenter
            {
                Name = "PART_HeaderPresenter",
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,

                [!ContentPresenter.MarginProperty] =
                    new TemplateBinding(TemplatedControl.PaddingProperty),

                [!ContentPresenter.ContentProperty] =
                    new TemplateBinding(MenuItem.HeaderProperty),

                [!ContentPresenter.ContentTemplateProperty] =
                    new TemplateBinding(MenuItem.HeaderTemplateProperty),
            },
            scope);

        // 保留 SimpleTheme 对 "_File" / "_Open" 等 AccessText 的支持。
        headerPresenter.DataTemplates.Add(
            new FuncDataTemplate<string>(
                (text, _) => new AccessText
                {
                    Text = text
                }));

        Grid.SetColumn(headerPresenter, 2);

        //
        // Ctrl+O / Ctrl+S 等 InputGesture
        //

        var inputGestureText = Register(
            new TextBlock
            {
                Name = "PART_InputGestureText",
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,

                [!TextBlock.TextProperty] =
                    new TemplateBinding(MenuItem.InputGestureProperty)
                    {
                        Converter = new PlatformKeyGestureConverter()
                    },
            },
            scope);

        inputGestureText.Bind(
            StyledElement.ThemeProperty,
            item.GetObservable(InputGestureTextThemeProperty));

        Grid.SetColumn(inputGestureText, 3);

        //
        // 子菜单右箭头
        //

        var rightArrow = Register(
            new Path
            {
                Name = "rightArrow",
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Data = Geometry.Parse("M0,0L4,3.5 0,7z"),
            },
            scope);

        rightArrow.Bind(
            Path.FillProperty,
            rightArrow.GetResourceObservable("ThemeForegroundBrush"));

        var rightArrowPresenter = new Border
        {
            Width = 20,
            [!Visual.IsVisibleProperty] =
                new TemplateBinding(ItemsControl.ItemCountProperty)
                {
                    Converter = PositiveIntToBoolConverter.Instance
                },
            Child = rightArrow
        };

        Grid.SetColumn(rightArrowPresenter, 4);

        //
        // 子菜单 ItemsPresenter
        //

        var itemsPresenter = Register(
            new ItemsPresenter
            {
                Name = "PART_ItemsPresenter",
                Margin = new Thickness(2),

                [!ItemsPresenter.ItemsPanelProperty] =
                    new TemplateBinding(ItemsControl.ItemsPanelProperty),
            },
            scope);

        itemsPresenter.SetValue(
            Grid.IsSharedSizeScopeProperty,
            true);

        //
        // ScrollViewer
        //

        var scrollViewer = new ScrollViewer
        {
            Content = itemsPresenter
        };

        // SimpleTheme 自己给菜单 Popup 使用的 ScrollViewer theme。
        if (item.TryFindResource(
                "SimpleMenuScrollViewer",
                item.ActualThemeVariant,
                out var resource)
            && resource is ControlTheme scrollViewerTheme)
        {
            scrollViewer.Theme = scrollViewerTheme;
        }

        //
        // Popup Border
        //

        var popupBorder = new Border
        {
            [!Border.BorderThicknessProperty] =
                new TemplateBinding(TemplatedControl.BorderThicknessProperty),

            Child = scrollViewer
        };

        popupBorder.Bind(
            Border.BackgroundProperty,
            popupBorder.GetResourceObservable("ThemeBackgroundBrush"));

        popupBorder.Bind(
            Border.BorderBrushProperty,
            popupBorder.GetResourceObservable("ThemeBorderMidBrush"));

        //
        // PART_Popup
        //
        // MenuItem.OnApplyTemplate() 会按名字找这个部件，
        // 所以必须注册进 template NameScope。
        //

        var popup = Register(
            new Popup
            {
                Name = "PART_Popup",
                IsLightDismissEnabled = false,
                Placement = PlacementMode.RightEdgeAlignedTop,

                [!Popup.IsOpenProperty] =
                    new TemplateBinding(MenuItem.IsSubMenuOpenProperty)
                    {
                        Mode = BindingMode.TwoWay
                    },

                Child = popupBorder
            },
            scope);

        //
        // Grid
        //

        grid.Children.Add(toggleIconPresenter);
        grid.Children.Add(iconPresenter);
        grid.Children.Add(headerPresenter);
        grid.Children.Add(inputGestureText);
        grid.Children.Add(rightArrowPresenter);
        grid.Children.Add(popup);

        //
        // root
        //

        return Register(
            new Border
            {
                Name = "root",

                [!Border.BackgroundProperty] =
                    new TemplateBinding(TemplatedControl.BackgroundProperty),

                [!Border.BorderBrushProperty] =
                    new TemplateBinding(TemplatedControl.BorderBrushProperty),

                [!Border.BorderThicknessProperty] =
                    new TemplateBinding(TemplatedControl.BorderThicknessProperty),

                [!Border.CornerRadiusProperty] =
                    new TemplateBinding(TemplatedControl.CornerRadiusProperty),

                Child = grid
            },
            scope);
    }

    private static T Register<T>(T control, INameScope scope)
        where T : Control
    {
        if (!string.IsNullOrEmpty(control.Name))
            scope.Register(control.Name, control);

        return control;
    }

    /// <summary>
    /// 模板内部的强类型绑定代理。两个 ColumnDefinition 不属于可视树，
    /// 因此通过它观察父 MenuItem 的 ShowIconColumn 设置。
    /// </summary>
    private sealed class LayoutGrid : Grid
    {
        public static readonly StyledProperty<bool> ShowIconColumnProperty =
            AvaloniaProperty.Register<LayoutGrid, bool>(
                nameof(ShowIconColumn),
                defaultValue: true);

        public bool ShowIconColumn
        {
            get => GetValue(ShowIconColumnProperty);
            set => SetValue(ShowIconColumnProperty, value);
        }
    }

    private sealed class BoolToGridLengthConverter : IValueConverter
    {
        public static readonly BoolToGridLengthConverter Instance = new();

        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
        {
            if (value is true && parameter is GridLength width)
                return width;

            return new GridLength(0);
        }

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
            => throw new NotSupportedException();
    }

    private sealed class PositiveIntToBoolConverter : IValueConverter
    {
        public static readonly PositiveIntToBoolConverter Instance = new();

        public object Convert(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
            => value is int count && count > 0;

        public object ConvertBack(
            object? value,
            Type targetType,
            object? parameter,
            CultureInfo culture)
            => throw new NotSupportedException();
    }
}
