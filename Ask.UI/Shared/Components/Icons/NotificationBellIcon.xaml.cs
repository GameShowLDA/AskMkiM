using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Shared.Components.Icons;

/// <summary>Иконка уведомлений на основе SemiIconBellStroked.</summary>
public partial class NotificationBellIcon : UserControl
{
  public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
    nameof(Size), typeof(double), typeof(NotificationBellIcon), new PropertyMetadata(18d));

  public double Size
  {
    get => (double)GetValue(SizeProperty);
    set => SetValue(SizeProperty, value);
  }

  public NotificationBellIcon() => InitializeComponent();
}
