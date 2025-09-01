using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace UI.Converter
{
  /// <summary> Умножает входное double-значение на коэффициент (ConverterParameter). </summary>
  public sealed class MultiplyConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is double d && parameter != null && double.TryParse(parameter.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var k))
        return d * k;
      return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
  }
}
