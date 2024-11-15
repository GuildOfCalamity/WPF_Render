using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace WPFRender;

/// <summary>
///  XAML DependencyObject method for smooth <see cref="System.Windows.Controls.ProgressBar"/>.
/// <para>
///  XAML usage:
/// <code>
///   &lt;ProgressBar local:ProgressBarSmoother.SmoothValue="{Binding ProgressAmount}"&gt;
/// </code>
/// </para>
/// </summary>
public static class ProgressBarSmoother
{
    const int _valueTransitionTime = 250; // in milliseconds

    public static double GetSmoothValue(DependencyObject obj)
    {
        if (obj == null)
            return 0d;

        return (double)obj.GetValue(SmoothValueProperty);
    }

    public static void SetSmoothValue(DependencyObject obj, double value)
    {
        if (obj == null)
            return;

        obj.SetValue(SmoothValueProperty, value);
    }

    public static readonly DependencyProperty SmoothValueProperty = DependencyProperty.RegisterAttached(
        "SmoothValue",
             typeof(double),
             typeof(ProgressBarSmoother),
             new PropertyMetadata(0.0, SmoothValueChanging));

    static void SmoothValueChanging(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue == null || (double)e.OldValue == double.NaN || e.NewValue == null || (double)e.NewValue == double.NaN)
            return;

        var anim = new DoubleAnimation((double)e.OldValue, (double)e.NewValue, new TimeSpan(0, 0, 0, 0, _valueTransitionTime));
        (d as ProgressBar)?.BeginAnimation(ProgressBar.ValueProperty, anim, HandoffBehavior.Compose);
    }
}
