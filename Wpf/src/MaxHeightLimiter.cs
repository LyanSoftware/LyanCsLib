using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Lytec.Wpf;

public static class MaxHeightLimiter
{
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.RegisterAttached("Target", typeof(FrameworkElement), typeof(MaxHeightLimiter),
            new PropertyMetadata(null, OnTargetChanged));

    public static void SetTarget(DependencyObject obj, FrameworkElement value) => obj.SetValue(TargetProperty, value);
    public static FrameworkElement GetTarget(DependencyObject obj) => (FrameworkElement)obj.GetValue(TargetProperty);

    private static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Panel panel && e.NewValue is FrameworkElement target)
        {
            void Update()
            {
                double max = 0;
                foreach (UIElement child in panel.Children)
                {
                    if (child == target || child is not FrameworkElement fe) continue;
                    fe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    max = Math.Max(max, fe.DesiredSize.Height);
                }
                target.MaxHeight = max;
            }

            panel.Loaded += (_, _) => Update();
            panel.LayoutUpdated += (_, _) => Update();
            panel.SizeChanged += (_, _) => Update();
        }
    }
}
