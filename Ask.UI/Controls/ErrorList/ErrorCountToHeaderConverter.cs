using System.Globalization;
using System.Windows.Data;

namespace Ask.UI.Controls.ErrorList
{
  public class ErrorCountToHeaderConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is int count)
        return $"{count}";
      return "0";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}

