using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Shared.Components.Icons;

/// <summary>Иконка списка дел на основе SemiIconChecklistStroked.</summary>
public partial class ChecklistIcon : UserControl
{
  public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
    nameof(Size), typeof(double), typeof(ChecklistIcon), new PropertyMetadata(18d));

  public double Size
  {
    get => (double)GetValue(SizeProperty);
    set => SetValue(SizeProperty, value);
  }

  public ChecklistIcon() => InitializeComponent();
}
