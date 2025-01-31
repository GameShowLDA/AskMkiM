using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace UI
{
  public class SliderValueToWidthConverter : IMultiValueConverter
  {
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values.Length < 4 || values[0] == DependencyProperty.UnsetValue) return 0;

      double value = (double)values[0];
      double min = (double)values[1];
      double max = (double)values[2];
      double actualWidth = (double)values[3];

      if (max <= min) return 0;

      return (value - min) / (max - min) * actualWidth;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
