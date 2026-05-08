using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ask.UI.Controls.ProtocolNew
{
  public class InverseBooleanToVisibilityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      bool b = value is bool boolVal && boolVal;
      return b ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return (value is Visibility visibility) && visibility != Visibility.Visible;
    }
  }
}
