using System.Globalization;
using System.Windows.Data;

namespace Ask.UI.Components.ProtocolListBox
{
  public class MessageToLeadingNewLineConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      string header = values[0] as string;
      string message = values[1] as string;

      bool isHeaderOnly = string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(header);
      bool isMultiLineHeader = !string.IsNullOrEmpty(header) && (header.Contains('\n') || header.Contains('\r'));
      return isHeaderOnly && isMultiLineHeader ? "\n" : "";
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}

