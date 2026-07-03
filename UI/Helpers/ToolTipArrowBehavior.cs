using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace UI.Helpers
{
  public static class ToolTipArrowBehavior
  {
    private const double ArrowWidth = 16d;
    private const double ArrowHeight = 8d;
    private const double ArrowMargin = 10d;
    private const double ArrowOverlap = 1d;

    public static readonly DependencyProperty IsEnabledProperty =
      DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(ToolTipArrowBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty ArrowLeftProperty =
      DependencyProperty.RegisterAttached(
        "ArrowLeft",
        typeof(double),
        typeof(ToolTipArrowBehavior),
        new PropertyMetadata(0d));

    public static readonly DependencyProperty ArrowTopProperty =
      DependencyProperty.RegisterAttached(
        "ArrowTop",
        typeof(double),
        typeof(ToolTipArrowBehavior),
        new PropertyMetadata(0d));

    public static readonly DependencyProperty ArrowAngleProperty =
      DependencyProperty.RegisterAttached(
        "ArrowAngle",
        typeof(double),
        typeof(ToolTipArrowBehavior),
        new PropertyMetadata(0d));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    public static double GetArrowLeft(DependencyObject obj) => (double)obj.GetValue(ArrowLeftProperty);

    public static void SetArrowLeft(DependencyObject obj, double value) => obj.SetValue(ArrowLeftProperty, value);

    public static double GetArrowTop(DependencyObject obj) => (double)obj.GetValue(ArrowTopProperty);

    public static void SetArrowTop(DependencyObject obj, double value) => obj.SetValue(ArrowTopProperty, value);

    public static double GetArrowAngle(DependencyObject obj) => (double)obj.GetValue(ArrowAngleProperty);

    public static void SetArrowAngle(DependencyObject obj, double value) => obj.SetValue(ArrowAngleProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is not ToolTip toolTip)
      {
        return;
      }

      if (e.NewValue is true)
      {
        toolTip.Opened += ToolTip_Opened;
        toolTip.SizeChanged += ToolTip_SizeChanged;
        toolTip.Placement = PlacementMode.Bottom;
        toolTip.VerticalOffset = 12;
      }
      else
      {
        toolTip.Opened -= ToolTip_Opened;
        toolTip.SizeChanged -= ToolTip_SizeChanged;
      }
    }

    private static void ToolTip_Opened(object sender, RoutedEventArgs e)
    {
      if (sender is ToolTip toolTip)
      {
        ScheduleUpdate(toolTip);
      }
    }

    private static void ToolTip_SizeChanged(object sender, SizeChangedEventArgs e)
    {
      if (sender is ToolTip toolTip && toolTip.IsOpen)
      {
        ScheduleUpdate(toolTip);
      }
    }

    private static void ScheduleUpdate(ToolTip toolTip)
    {
      toolTip.Dispatcher.BeginInvoke(
        DispatcherPriority.Loaded,
        new Action(() => UpdateArrow(toolTip)));
    }

    private static void UpdateArrow(ToolTip toolTip)
    {
      if (toolTip.PlacementTarget is not FrameworkElement target ||
          toolTip.ActualWidth <= 0 ||
          toolTip.ActualHeight <= 0 ||
          target.ActualWidth <= 0 ||
          target.ActualHeight <= 0)
      {
        SetArrowLeft(toolTip, Math.Max(ArrowMargin, (toolTip.ActualWidth - ArrowWidth) / 2));
        SetArrowTop(toolTip, 0);
        SetArrowAngle(toolTip, 0);
        return;
      }

      Rect targetRect = GetScreenRect(target);
      Rect toolTipRect = GetScreenRect(toolTip);
      Point targetCenter = new(
        targetRect.Left + targetRect.Width / 2,
        targetRect.Top + targetRect.Height / 2);

      if (targetCenter.Y <= toolTipRect.Top)
      {
        SetArrowLeft(toolTip, Clamp(targetCenter.X - toolTipRect.Left - ArrowWidth / 2, ArrowMargin, toolTip.ActualWidth - ArrowWidth - ArrowMargin));
        SetArrowTop(toolTip, ArrowOverlap);
        SetArrowAngle(toolTip, 0);
        return;
      }

      if (targetCenter.Y >= toolTipRect.Bottom)
      {
        SetArrowLeft(toolTip, Clamp(targetCenter.X - toolTipRect.Left - ArrowWidth / 2, ArrowMargin, toolTip.ActualWidth - ArrowWidth - ArrowMargin));
        SetArrowTop(toolTip, Math.Max(0, toolTip.ActualHeight - ArrowHeight - ArrowOverlap));
        SetArrowAngle(toolTip, 180);
        return;
      }

      if (targetCenter.X <= toolTipRect.Left)
      {
        SetArrowLeft(toolTip, ArrowOverlap);
        SetArrowTop(toolTip, Clamp(targetCenter.Y - toolTipRect.Top - ArrowWidth / 2, ArrowMargin, toolTip.ActualHeight - ArrowWidth - ArrowMargin));
        SetArrowAngle(toolTip, -90);
        return;
      }

      SetArrowLeft(toolTip, Math.Max(0, toolTip.ActualWidth - ArrowHeight - ArrowOverlap));
      SetArrowTop(toolTip, Clamp(targetCenter.Y - toolTipRect.Top - ArrowWidth / 2, ArrowMargin, toolTip.ActualHeight - ArrowWidth - ArrowMargin));
      SetArrowAngle(toolTip, 90);
    }

    private static Rect GetScreenRect(FrameworkElement element)
    {
      Point screenTopLeft = element.PointToScreen(new Point(0, 0));
      Point screenBottomRight = element.PointToScreen(new Point(element.ActualWidth, element.ActualHeight));

      PresentationSource? source = PresentationSource.FromVisual(element);
      if (source?.CompositionTarget != null)
      {
        screenTopLeft = source.CompositionTarget.TransformFromDevice.Transform(screenTopLeft);
        screenBottomRight = source.CompositionTarget.TransformFromDevice.Transform(screenBottomRight);
      }

      return new Rect(screenTopLeft, screenBottomRight);
    }

    private static double Clamp(double value, double min, double max)
    {
      if (max < min)
      {
        return Math.Max(0, value);
      }

      return Math.Min(Math.Max(value, min), max);
    }
  }
}
