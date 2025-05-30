using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UI.Components.ProtocolListBox
{
  public class IndentToMarginConverter : IValueConverter
  {
    private const double IndentSize = 20.0; // ширина одного уровня отступа

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is int indent)
        return new Thickness(indent * IndentSize, 0, 0, 0);

      return new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
  }
}
