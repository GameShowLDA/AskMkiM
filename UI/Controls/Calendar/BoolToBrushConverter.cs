using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace UI.Controls.Calendar
{
  public class BoolToBrushConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      bool val = (bool)value;
      if (parameter?.ToString() == "Highlight")
        return val ? new SolidColorBrush(Color.FromRgb(0, 120, 215)) : Brushes.Transparent;
      if (parameter?.ToString() == "Foreground")
        return val ? Brushes.White : Brushes.LightGray;
      return Brushes.Transparent;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
  }

  public class BoolToOpacityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (bool)value ? 1.0 : 0.5;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
  }

  public class BoolToFontWeightConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (bool)value ? FontWeights.Bold : FontWeights.Normal;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
  }
}
