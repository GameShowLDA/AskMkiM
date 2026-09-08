using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Shared.Components.Icons;

/// <summary>Иконка выгрузки диагностического отчёта на основе SemiIconDownload.</summary>
public partial class DailyReportIcon : UserControl
{
  public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
    nameof(Size), typeof(double), typeof(DailyReportIcon), new PropertyMetadata(18d));

  public double Size
  {
    get => (double)GetValue(SizeProperty);
    set => SetValue(SizeProperty, value);
  }

  public DailyReportIcon() => InitializeComponent();
}
