using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace UI.Controls.EmptyWorkspace
{
  public class TimeOfDayGreetingConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is not DateTime dt) return string.Empty;
      int h = dt.Hour;
      string baseStr = " Запустить последнюю сессию?";
      // Диапазоны можно подстроить под себя
      if (h >= 5 && h < 11) return "Доброе утро." + baseStr;
      if (h >= 11 && h < 17) return "Добрый день." + baseStr;
      if (h >= 17 && h < 23) return "Добрый вечер." + baseStr;
      return "Доброй ночи." + baseStr;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
      => throw new NotSupportedException();
  }
}
