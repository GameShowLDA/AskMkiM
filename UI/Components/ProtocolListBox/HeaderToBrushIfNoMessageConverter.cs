using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using Utilities.Models;

namespace UI.Components.ProtocolListBox
{
  public class HeaderToBrushIfNoMessageConverter : IMultiValueConverter
  {
    // Зеленый цвет "НОРМА"
    private static readonly SolidColorBrush SuccessBrush = new SolidColorBrush(ShowMessageModel.SuccessMessage.TitleColor);

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      string header = values[0] as string;
      string message = values[1] as string;
      SolidColorBrush headerColor = values[2] as SolidColorBrush;

      if (!string.IsNullOrEmpty(header) && string.IsNullOrEmpty(message))
      {
        return SuccessBrush;
      }
      return headerColor ?? new SolidColorBrush(Colors.White); // Цвет по умолчанию
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
