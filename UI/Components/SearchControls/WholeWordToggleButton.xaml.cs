using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UI.Components.SearchControls
{
  public partial class WholeWordToggleButton : UserControl
  {
    public WholeWordToggleButton()
    {
      InitializeComponent();
    }

    public override void OnApplyTemplate()
    {
      base.OnApplyTemplate();
      UpdateVisualState();
    }

    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(WholeWordToggleButton),
            new PropertyMetadata(false, OnIsCheckedChanged));

    public bool IsChecked
    {
      get => (bool)GetValue(IsCheckedProperty);
      set => SetValue(IsCheckedProperty, value);
    }

    private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is WholeWordToggleButton control)
      {
        control.UpdateVisualState();
      }
    }

    public static readonly DependencyProperty HoverTextColorProperty =
        DependencyProperty.Register(nameof(HoverTextColor), typeof(Brush), typeof(WholeWordToggleButton),
            new PropertyMetadata(Brushes.White));

    public Brush HoverTextColor
    {
      get => (Brush)GetValue(HoverTextColorProperty);
      set => SetValue(HoverTextColorProperty, value);
    }

    public static readonly DependencyProperty ForegroundProperty =
        DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(WholeWordToggleButton),
            new PropertyMetadata(Brushes.Gray, OnForegroundChanged));

    public static readonly DependencyProperty ActiveForegroundProperty =
        DependencyProperty.Register(nameof(ActiveForeground), typeof(Brush), typeof(WholeWordToggleButton),
            new PropertyMetadata(Brushes.Green));

    public Brush ActiveForeground
    {
      get => (Brush)GetValue(ActiveForegroundProperty);
      set => SetValue(ActiveForegroundProperty, value);
    }

    private static void OnForegroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
      if (d is WholeWordToggleButton control)
      {
        control.UpdateVisualState();
      }
    }

    private void ToggleButton_Click(object sender, RoutedEventArgs e)
    {
      if (!IsEnabled) return;
      IsChecked = !IsChecked;
      UpdateVisualState();
    }

    private void UpdateVisualState()
    {
      ButtonBorder.BorderThickness = IsChecked ? new Thickness(1.5) : new Thickness(0);
      ButtonBorder.BorderBrush = IsChecked ? ActiveForeground : Foreground;
    }
  }
}
