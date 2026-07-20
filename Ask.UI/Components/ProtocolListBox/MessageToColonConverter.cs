using System.Globalization;
using System.Windows.Data;

namespace Ask.UI.Components.ProtocolListBox
{
  public class MessageToColonConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      var header = values.Length > 0 ? values[0] as string : string.Empty;
      var message = values.Length > 1 ? values[1] as string : string.Empty;

      return string.IsNullOrWhiteSpace(header) || string.IsNullOrWhiteSpace(message)
        ? string.Empty
        : ":";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
