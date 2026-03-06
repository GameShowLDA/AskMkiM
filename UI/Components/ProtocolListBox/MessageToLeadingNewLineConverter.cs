using System.Globalization;
using System.Windows.Data;

namespace UI.Components.ProtocolListBox
{
  public class MessageToLeadingNewLineConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      string header = values[0] as string;
      string message = values[1] as string;

      return string.IsNullOrEmpty(message) ? !string.IsNullOrEmpty(header) ? "\n" : "" : "";
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
