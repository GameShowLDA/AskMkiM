using System.Globalization;
using System.Windows.Data;

namespace UI.Components.ProtocolListBox
{
  public class TimeToSeparatorConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var time = value as string;
      return string.IsNullOrEmpty(time) ? "" : "|";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }

}
