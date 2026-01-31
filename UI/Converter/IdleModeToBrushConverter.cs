using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UI.Converter
{
  public class IdleModeToBrushConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is bool isIdle && isIdle)
        return Application.Current.Resources["GreenColorSolidColorBrush"];

      return Application.Current.Resources["ActiveBorderSolidColorBrush"];
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      => Binding.DoNothing;
  }
}
