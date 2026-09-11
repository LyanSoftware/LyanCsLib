using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;

namespace Lytec.AvaloniaUI.Mvu;

public enum DrawerEdge
{
    Left,
    Top,
    Right,
    Bottom,
}

public sealed class NavigationHostOptions
{
    public bool HasNavigationBarShadow { get; set; } = true;

    public string? DesktopTitle { get; set; }

    /// <summary>
    /// Used only when navigation presentation is hosted by a desktop window.
    /// </summary>
    public WindowInfo DesktopWindow { get; set; } = new();
}

public interface IDrawerTransition
{
    Task RunAsync(
        Control drawer,
        DrawerEdge edge,
        bool opening,
        double extent,
        TimeSpan duration,
        CancellationToken cancellationToken);
}

/// <summary>
/// Default edge-aware slide animation for a navigation drawer.
/// </summary>
public sealed class SlideDrawerTransition : IDrawerTransition
{
    public static SlideDrawerTransition Instance { get; } = new();

    public async Task RunAsync(
        Control drawer,
        DrawerEdge edge,
        bool opening,
        double extent,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        var offset = edge switch
        {
            DrawerEdge.Left => -extent,
            DrawerEdge.Top => -extent,
            DrawerEdge.Right => extent,
            DrawerEdge.Bottom => extent,
            _ => throw new ArgumentOutOfRangeException(nameof(edge)),
        };
        static TransformOperations Transform(
            DrawerEdge edge,
            double value)
            => TransformOperations.Parse(
                edge is DrawerEdge.Left or DrawerEdge.Right
                    ? $"translate({value.ToString(System.Globalization.CultureInfo.InvariantCulture)}px, 0px)"
                    : $"translate(0px, {value.ToString(System.Globalization.CultureInfo.InvariantCulture)}px)");

        drawer.Transitions = null;
        drawer.RenderTransform = Transform(edge, opening ? offset : 0d);
        await drawer.Dispatcher.InvokeAsync(
            static () => { },
            DispatcherPriority.Render);

        drawer.Transitions = new Transitions
        {
            new TransformOperationsTransition
            {
                Property = Visual.RenderTransformProperty,
                Duration = duration,
            },
        };
        drawer.RenderTransform = Transform(edge, opening ? 0d : offset);
        try
        {
            await Task.Delay(duration, cancellationToken);
        }
        finally
        {
            drawer.Transitions = null;
        }
    }
}

/// <summary>
/// Bindable presentation options for one registered drawer.
/// </summary>
public sealed class DrawerOptions : AvaloniaObject
{
    public static readonly StyledProperty<DrawerEdge> EdgeProperty =
        AvaloniaProperty.Register<DrawerOptions, DrawerEdge>(nameof(Edge));

    public static readonly StyledProperty<GridLength> SizeProperty =
        AvaloniaProperty.Register<DrawerOptions, GridLength>(
            nameof(Size),
            new GridLength(0.8, GridUnitType.Star));

    public static readonly StyledProperty<double> MinSizeProperty =
        AvaloniaProperty.Register<DrawerOptions, double>(nameof(MinSize));

    public static readonly StyledProperty<double> MaxSizeProperty =
        AvaloniaProperty.Register<DrawerOptions, double>(
            nameof(MaxSize),
            double.PositiveInfinity);

    public static readonly StyledProperty<bool> IsLightDismissEnabledProperty =
        AvaloniaProperty.Register<DrawerOptions, bool>(
            nameof(IsLightDismissEnabled),
            true);

    public static readonly StyledProperty<IBrush?> BackdropProperty =
        AvaloniaProperty.Register<DrawerOptions, IBrush?>(
            nameof(Backdrop),
            new SolidColorBrush(Color.FromArgb(0x66, 0, 0, 0)));

    public static readonly StyledProperty<bool> IsAnimationEnabledProperty =
        AvaloniaProperty.Register<DrawerOptions, bool>(
            nameof(IsAnimationEnabled),
            true);

    public static readonly StyledProperty<TimeSpan> AnimationDurationProperty =
        AvaloniaProperty.Register<DrawerOptions, TimeSpan>(
            nameof(AnimationDuration),
            TimeSpan.FromMilliseconds(220));

    public static readonly StyledProperty<IDrawerTransition?> TransitionProperty =
        AvaloniaProperty.Register<DrawerOptions, IDrawerTransition?>(
            nameof(Transition),
            SlideDrawerTransition.Instance);

    public DrawerEdge Edge
    {
        get => GetValue(EdgeProperty);
        set => SetValue(EdgeProperty, value);
    }

    /// <summary>
    /// Absolute values are device-independent pixels, Auto uses desired size,
    /// and Star values are interpreted as a fraction of the available axis.
    /// </summary>
    public GridLength Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public double MinSize
    {
        get => GetValue(MinSizeProperty);
        set => SetValue(MinSizeProperty, value);
    }

    public double MaxSize
    {
        get => GetValue(MaxSizeProperty);
        set => SetValue(MaxSizeProperty, value);
    }

    public bool IsLightDismissEnabled
    {
        get => GetValue(IsLightDismissEnabledProperty);
        set => SetValue(IsLightDismissEnabledProperty, value);
    }

    /// <summary>
    /// A non-interactive brush covering main content outside the drawer. An
    /// ImageBrush may be used for a logo or background artwork.
    /// </summary>
    public IBrush? Backdrop
    {
        get => GetValue(BackdropProperty);
        set => SetValue(BackdropProperty, value);
    }

    public bool IsAnimationEnabled
    {
        get => GetValue(IsAnimationEnabledProperty);
        set => SetValue(IsAnimationEnabledProperty, value);
    }

    public TimeSpan AnimationDuration
    {
        get => GetValue(AnimationDurationProperty);
        set => SetValue(AnimationDurationProperty, value);
    }

    public IDrawerTransition? Transition
    {
        get => GetValue(TransitionProperty);
        set => SetValue(TransitionProperty, value);
    }
}

internal sealed class MultiDrawerHost : Grid
{
    private readonly ContentPresenter mainPresenter = new();
    private readonly Border backdrop = new() { IsVisible = false };
    private readonly Border drawerPresenter = new() { IsVisible = false };
    private DrawerOptions? currentOptions;
    private IInputElement? previousFocus;

    public MultiDrawerHost()
    {
        Children.Add(mainPresenter);
        Children.Add(backdrop);
        Children.Add(drawerPresenter);
        mainPresenter.ZIndex = 0;
        backdrop.ZIndex = 1;
        drawerPresenter.ZIndex = 2;
        backdrop.PointerPressed += OnBackdropPointerPressed;
    }

    public event EventHandler? LightDismissRequested;

    public void SetMainContent(Control content)
        => mainPresenter.Content = content;

    public async Task ShowAsync(
        Control drawer,
        DrawerOptions options,
        CancellationToken cancellationToken)
    {
        previousFocus = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
        SetCurrentOptions(options);
        drawerPresenter.Child = drawer;
        drawerPresenter.RenderTransform = null;
        ApplyOptions();
        backdrop.Opacity = 1;
        backdrop.IsVisible = true;
        drawerPresenter.IsVisible = true;

        var duration = options.AnimationDuration < TimeSpan.Zero
            ? TimeSpan.Zero
            : options.AnimationDuration;
        if (duration > TimeSpan.Zero
            && IsEffectivelyVisible
            && options.IsAnimationEnabled
            && options.Transition is { } transition)
        {
            await Dispatcher.InvokeAsync(UpdateLayout);
            await transition.RunAsync(
                drawerPresenter,
                options.Edge,
                opening: true,
                GetExtent(options),
                duration,
                cancellationToken);
        }

        drawer.Focus();
    }

    public async Task HideAsync(CancellationToken cancellationToken)
    {
        var options = currentOptions;
        if (options is null)
            return;

        var duration = options.AnimationDuration < TimeSpan.Zero
            ? TimeSpan.Zero
            : options.AnimationDuration;
        if (duration > TimeSpan.Zero
            && IsEffectivelyVisible
            && options.IsAnimationEnabled
            && options.Transition is { } transition)
        {
            await transition.RunAsync(
                drawerPresenter,
                options.Edge,
                opening: false,
                GetExtent(options),
                duration,
                cancellationToken);
        }

        backdrop.IsVisible = false;
        drawerPresenter.IsVisible = false;
        drawerPresenter.RenderTransform = null;
        SetCurrentOptions(null);
        previousFocus?.Focus();
        previousFocus = null;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BoundsProperty && currentOptions is not null)
            ApplyOptions();
    }

    private void SetCurrentOptions(DrawerOptions? options)
    {
        if (currentOptions is not null)
            currentOptions.PropertyChanged -= OnOptionsChanged;
        currentOptions = options;
        if (currentOptions is not null)
            currentOptions.PropertyChanged += OnOptionsChanged;
    }

    private void OnOptionsChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        => ApplyOptions();

    private void ApplyOptions()
    {
        if (currentOptions is not { } options)
            return;

        backdrop.Background = options.Backdrop ?? Brushes.Transparent;
        drawerPresenter.HorizontalAlignment = options.Edge switch
        {
            DrawerEdge.Left => HorizontalAlignment.Left,
            DrawerEdge.Right => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Stretch,
        };
        drawerPresenter.VerticalAlignment = options.Edge switch
        {
            DrawerEdge.Top => VerticalAlignment.Top,
            DrawerEdge.Bottom => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Stretch,
        };

        var minSize = Math.Max(0, options.MinSize);
        var maxSize = Math.Max(minSize, options.MaxSize);
        var extent = ResolveExtent(options, minSize, maxSize);
        if (options.Edge is DrawerEdge.Left or DrawerEdge.Right)
        {
            drawerPresenter.Width = extent;
            drawerPresenter.Height = double.NaN;
            drawerPresenter.MinWidth = minSize;
            drawerPresenter.MaxWidth = maxSize;
        }
        else
        {
            drawerPresenter.Width = double.NaN;
            drawerPresenter.Height = extent;
            drawerPresenter.MinHeight = minSize;
            drawerPresenter.MaxHeight = maxSize;
        }
    }

    private double ResolveExtent(
        DrawerOptions options,
        double? normalizedMin = null,
        double? normalizedMax = null)
    {
        var min = normalizedMin ?? Math.Max(0, options.MinSize);
        var max = normalizedMax ?? Math.Max(min, options.MaxSize);
        var size = options.Size;
        if (size.IsAuto)
            return double.NaN;
        if (size.IsAbsolute)
            return Math.Clamp(size.Value, min, max);

        var available = options.Edge is DrawerEdge.Left or DrawerEdge.Right
            ? Bounds.Width
            : Bounds.Height;
        var fraction = Math.Clamp(size.Value, 0, 1);
        return Math.Clamp(available * fraction, min, max);
    }

    private double GetExtent(DrawerOptions options)
    {
        var actual = options.Edge is DrawerEdge.Left or DrawerEdge.Right
            ? drawerPresenter.Bounds.Width
            : drawerPresenter.Bounds.Height;
        if (actual > 0)
            return actual;

        var resolved = ResolveExtent(options);
        return double.IsNaN(resolved) ? 0 : resolved;
    }

    private void OnBackdropPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
        if (currentOptions?.IsLightDismissEnabled == true)
            LightDismissRequested?.Invoke(this, EventArgs.Empty);
    }
}
