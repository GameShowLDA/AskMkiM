using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Shared.Components.Icons;

/// <summary>Иконка календаря на основе SemiIconCalendar.</summary>
public partial class CalendarIcon : UserControl
{
  public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
    nameof(Size), typeof(double), typeof(CalendarIcon), new PropertyMetadata(18d));

  public double Size
  {
    get => (double)GetValue(SizeProperty);
    set => SetValue(SizeProperty, value);
  }

  public CalendarIcon() => InitializeComponent();
}
