using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Lytec.Wpf;

public static class MaxHeightLimiter
{
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.RegisterAttached(
            "Target",
            typeof(FrameworkElement),
            typeof(MaxHeightLimiter),
            new PropertyMetadata(null, OnTargetChanged));

    private static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached(
            "State",
            typeof(State),
            typeof(MaxHeightLimiter));

    public static void SetTarget(
        DependencyObject obj,
        FrameworkElement? value)
        => obj.SetValue(TargetProperty, value);

    public static FrameworkElement? GetTarget(DependencyObject obj)
        => (FrameworkElement?)obj.GetValue(TargetProperty);

    private static void OnTargetChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is not Panel panel)
            return;

        // 清理旧的监听，避免重复订阅。
        if (panel.GetValue(StateProperty) is State oldState)
        {
            oldState.Dispose();
            panel.ClearValue(StateProperty);
        }

        if (e.NewValue is not FrameworkElement target)
            return;

        var state = new State(panel, target);
        panel.SetValue(StateProperty, state);
        state.Attach();
    }

    private sealed class State : IDisposable
    {
        private readonly Panel panel;
        private readonly FrameworkElement target;

        private bool updatePending;
        private bool disposed;

        public State(Panel panel, FrameworkElement target)
        {
            this.panel = panel;
            this.target = target;
        }

        public void Attach()
        {
            panel.Loaded += OnChanged;
            panel.SizeChanged += OnChanged;

            foreach (var child in panel.Children
                         .OfType<FrameworkElement>())
            {
                if (child != target)
                    child.SizeChanged += OnChanged;
            }

            QueueUpdate();
        }

        private void OnChanged(object? sender, EventArgs e)
        {
            QueueUpdate();
        }

        private void QueueUpdate()
        {
            if (disposed || updatePending)
                return;

            updatePending = true;

            panel.Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(() =>
                {
                    updatePending = false;

                    if (!disposed)
                        Update();
                }));
        }

        private void Update()
        {
            if (!panel.IsLoaded)
                return;

            var maxHeight = panel.Children
                .OfType<FrameworkElement>()
                .Where(x =>
                    x != target &&
                    x.Visibility != Visibility.Collapsed)
                .Select(x => x.ActualHeight)
                .DefaultIfEmpty(0)
                .Max();

            if (maxHeight <= 0)
                return;

            // 防止浮点抖动和无意义的布局失效。
            if (double.IsPositiveInfinity(target.MaxHeight) ||
                Math.Abs(target.MaxHeight - maxHeight) > 0.5)
            {
                target.MaxHeight = maxHeight;
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            panel.Loaded -= OnChanged;
            panel.SizeChanged -= OnChanged;

            foreach (var child in panel.Children
                         .OfType<FrameworkElement>())
            {
                if (child != target)
                    child.SizeChanged -= OnChanged;
            }
        }
    }
}
