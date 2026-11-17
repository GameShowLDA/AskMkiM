using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Ask.UI.Features.TextSearch.Views;

public partial class SearchPanelWindow
{
  private bool _closingAnimation;
  private int _motionVersion;
  private readonly System.Collections.Generic.Dictionary<FrameworkElement, bool> _sectionStates = new();
  private readonly System.Collections.Generic.Dictionary<FrameworkElement, Thickness> _sectionMargins = new();

  private bool IsSectionExpanded(FrameworkElement section) =>
    _sectionStates.TryGetValue(section, out bool expanded) ? expanded : section.Visibility == Visibility.Visible;

  private void SetSectionExpanded(FrameworkElement section, bool expanded)
  {
    bool wasExpanded = IsSectionExpanded(section);
    _sectionStates[section] = expanded;
    if (wasExpanded == expanded) return;
    double fromHeight = section.Visibility == Visibility.Visible ? section.ActualHeight : 0;
    double fromOpacity = section.Visibility == Visibility.Visible ? section.Opacity : 0;
    if (!_sectionMargins.TryGetValue(section, out var naturalMargin))
      _sectionMargins[section] = naturalMargin = section.Margin;
    var fromMargin = section.Visibility == Visibility.Visible ? section.Margin
      : new Thickness(naturalMargin.Left, 0, naturalMargin.Right, 0);
    section.BeginAnimation(FrameworkElement.HeightProperty, null);
    section.BeginAnimation(UIElement.OpacityProperty, null);
    section.BeginAnimation(FrameworkElement.MarginProperty, null);
    section.Margin = naturalMargin;
    section.Height = double.NaN;
    section.Visibility = Visibility.Visible;
    section.ClipToBounds = true;
    section.IsHitTestVisible = expanded;
    double availableWidth = Math.Max(1, PanelShell.ActualWidth - PanelShell.Padding.Left - PanelShell.Padding.Right);
    section.Measure(new Size(availableWidth, double.PositiveInfinity));
    double toHeight = expanded ? Math.Max(0, section.DesiredSize.Height - section.Margin.Top - section.Margin.Bottom) : 0;
    var height = CreateSectionAnimation(fromHeight, toHeight);
    height.Completed += (_, _) =>
    {
      // A reversed transition must not complete an older collapse.
      if (IsSectionExpanded(section) != expanded) return;
      section.BeginAnimation(FrameworkElement.HeightProperty, null);
      section.Height = double.NaN;
      section.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
      section.BeginAnimation(FrameworkElement.MarginProperty, null);
      section.Margin = naturalMargin;
      section.BeginAnimation(UIElement.OpacityProperty, null);
      section.Opacity = 1;
      UpdateShellCorners();
    };
    section.BeginAnimation(FrameworkElement.HeightProperty, height);
    section.BeginAnimation(UIElement.OpacityProperty, CreateSectionAnimation(fromOpacity, expanded ? 1 : 0));
    section.BeginAnimation(FrameworkElement.MarginProperty, new ThicknessAnimation(fromMargin,
      expanded ? naturalMargin : new Thickness(naturalMargin.Left, 0, naturalMargin.Right, 0),
      TimeSpan.FromMilliseconds(SystemParameters.ClientAreaAnimation ? 280 : 0))
    {
      EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
    });
  }

  private static DoubleAnimation CreateSectionAnimation(double from, double to) => new(from, to,
    TimeSpan.FromMilliseconds(SystemParameters.ClientAreaAnimation ? 280 : 0))
  {
    EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
  };

  private void PrepareOpening()
  {
    bool reversing = _closingAnimation;
    _closingAnimation = false;
    _motionVersion++;
    MotionRoot.IsHitTestVisible = true;
    if (reversing) return;
    MotionRoot.Opacity = 0;
    PanelScale.ScaleX = PanelScale.ScaleY = 0.96;
  }

  private void AnimateOpening()
  {
    Animate(MotionRoot, UIElement.OpacityProperty, 1, 180);
    Animate(PanelScale, ScaleTransform.ScaleXProperty, 1, 220);
    Animate(PanelScale, ScaleTransform.ScaleYProperty, 1, 220);
  }

  public void CloseAnimated()
  {
    if (_closed || _closingAnimation) return;
    if (!IsVisible || !SystemParameters.ClientAreaAnimation) { Close(); return; }
    _closingAnimation = true;
    int version = ++_motionVersion;
    // The window must keep intercepting input until Close; otherwise a second
    // click can reach the owner's title-bar buttons underneath the fading panel.
    Animate(PanelScale, ScaleTransform.ScaleXProperty, 0.97, 130);
    Animate(PanelScale, ScaleTransform.ScaleYProperty, 0.97, 130);
    var fade = CreateAnimation(MotionRoot.Opacity, 0, 130);
    fade.Completed += (_, _) =>
    {
      if (!_closed && _closingAnimation && version == _motionVersion) Close();
    };
    MotionRoot.BeginAnimation(UIElement.OpacityProperty, fade);
  }

  private static void Animate(Animatable target, DependencyProperty property, double to, int milliseconds)
  {
    double from = (double)target.GetValue(property);
    target.BeginAnimation(property, CreateAnimation(from, to, milliseconds));
  }

  private static void Animate(UIElement target, DependencyProperty property, double to, int milliseconds)
  {
    double from = (double)target.GetValue(property);
    target.BeginAnimation(property, CreateAnimation(from, to, milliseconds));
  }

  private static DoubleAnimation CreateAnimation(double from, double to, int milliseconds) => new(from, to,
    TimeSpan.FromMilliseconds(SystemParameters.ClientAreaAnimation ? milliseconds : 0))
  {
    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
  };
}
