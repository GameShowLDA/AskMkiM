using System.Globalization;
using System.Windows.Data;

namespace UI.Components.ProtocolListBox
{
  public class MessageToColonConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var message = value as string;
      return string.IsNullOrEmpty(message) ? "" : ":";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
