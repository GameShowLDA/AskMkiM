using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace UI.Components.SearchControls
{
  public partial class ArrowButton : UserControl
  {
    private RotateTransform _rootRotate;

    public ArrowButton()
    {
      InitializeComponent();
      this.DataContext = this;
    }

    public override void OnApplyTemplate()
    {
      base.OnApplyTemplate();

      _rootRotate = this.RenderTransform as RotateTransform;

      RotateArrow(IsArrowUp);
    }

    public static readonly DependencyProperty ArrowColorProperty =
        DependencyProperty.Register(nameof(ArrowColor), typeof(Brush), typeof(ArrowButton),
            new PropertyMetadata(Brushes.White));

    public Brush ArrowColor
    {
      get => (Brush)GetValue(ArrowColorProperty);
      set => SetValue(ArrowColorProperty, value);
    }

    public static readonly DependencyProperty HoverArrowColorProperty =
        DependencyProperty.Register(nameof(HoverArrowColor), typeof(Brush), typeof(ArrowButton),
            new PropertyMetadata(Brushes.Gray));

    public Brush HoverArrowColor
    {
      get => (Brush)GetValue(HoverArrowColorProperty);
      set => SetValue(HoverArrowColorProperty, value);
    }

    public static readonly DependencyProperty IsArrowUpProperty =
        DependencyProperty.Register(nameof(IsArrowUp), typeof(bool), typeof(ArrowButton),
            new PropertyMetadata(false, OnIsArrowUpChanged));

    public bool IsArrowUp
    {
      get => (bool)GetValue(IsArrowUpProperty);
      set => SetValue(IsArrowUpProperty, value);
    }

    private static void OnIsArrowUpChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is ArrowButton control)
      {
        control.RotateArrow((bool)e.NewValue);
      }
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
      IsArrowUp = !IsArrowUp;
      RotateArrow(IsArrowUp);
    }

    private void RotateArrow(bool isUp)
    {
      if (_rootRotate == null) return;

      double targetAngle = isUp ? 180 : 0;

      DoubleAnimation animation = new DoubleAnimation(targetAngle, TimeSpan.FromMilliseconds(200))
      {
        EasingFunction = new QuadraticEase()
      };
      _rootRotate.BeginAnimation(RotateTransform.AngleProperty, animation);
    }
  }
}
