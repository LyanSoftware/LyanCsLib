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
    /// 是否保留 MenuItem 左侧的 Icon / Check / Radio 列。
    ///
    /// 此属性可继承，因此可以直接设置在 Menu 上，
    /// 所有子 MenuItem 都会继承。
    /// </summary>
    public static readonly AttachedProperty<bool> ShowIconColumnProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>(
            "ShowIconColumn",
            typeof(MenuItemLayout),
            defaultValue: true,
            inherits: true);

    public static bool GetShowIconColumn(Control control)
        => control.GetValue(ShowIconColumnProperty);

    public static void SetShowIconColumn(Control control, bool value)
        => control.SetValue(ShowIconColumnProperty, value);

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

        // ColumnDefinition 不在模板的可视树中，无法直接解析 TemplateBinding。
        // 先由 Grid（模板可视元素）接收值，再让列定义绑定到这个代理值。
        var grid = new Grid
        {
            [!Control.TagProperty] = new TemplateBinding(ShowIconColumnProperty)
        };

        var iconColumn = new ColumnDefinition();

        iconColumn[!ColumnDefinition.WidthProperty] =
            new Binding(nameof(Control.Tag))
            {
                Source = grid,
                Converter = BoolToGridLengthConverter.Instance,
                ConverterParameter = new GridLength(20),
            };

        var iconSpacerColumn = new ColumnDefinition();

        iconSpacerColumn[!ColumnDefinition.WidthProperty] =
            new Binding(nameof(Control.Tag))
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
            Width = new GridLength(20)
        };

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

        Grid.SetColumn(rightArrow, 4);

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
        grid.Children.Add(rightArrow);
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
}
