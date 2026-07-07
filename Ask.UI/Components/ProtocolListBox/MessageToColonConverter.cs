using System.Globalization;
using System.Windows.Data;

namespace Ask.UI.Components.ProtocolListBox
{
  public class MessageToColonConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var message = value as string;
      if (string.IsNullOrEmpty(message))
      {
        return "";
      }

      var trimmedMessage = message.TrimStart();
      return trimmedMessage.StartsWith("$TST", StringComparison.Ordinal) ||
             trimmedMessage.StartsWith("$DOC", StringComparison.Ordinal)
        ? ""
        : ":";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
