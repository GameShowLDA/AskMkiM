using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace UI
{
  /// <summary>
  /// Конвертер значений для преобразования значения слайдера в ширину элемента UI.
  /// </summary>
  public class SliderValueToWidthConverter : IMultiValueConverter
  {
    /// <summary>
    /// Преобразует значение слайдера в соответствующую ширину элемента.
    /// </summary>
    /// <param name="values">Массив входных значений:
    /// 0 - текущее значение слайдера,
    /// 1 - минимальное значение слайдера,
    /// 2 - максимальное значение слайдера,
    /// 3 - текущая ширина элемента.</param>
    /// <param name="targetType">Тип целевого свойства привязки.</param>
    /// <param name="parameter">Дополнительный параметр преобразования.</param>
    /// <param name="culture">Культура для преобразования.</param>
    /// <returns>Рассчитанная ширина элемента UI, соответствующая значению слайдера.</returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values.Length < 4 || values[0] == DependencyProperty.UnsetValue)
      {
        return 0;
      }

      double value = (double)values[0];
      double min = (double)values[1];
      double max = (double)values[2];
      double actualWidth = (double)values[3];

      if (max <= min)
      {
        return 0;
      }

      return (value - min) / (max - min) * actualWidth;
    }

    /// <summary>
    /// Преобразует значение слайдера в соответствующую ширину элемента.
    /// </summary>
    /// <param name="value">Массив входных значений:
    /// 0 - текущее значение слайдера,
    /// 1 - минимальное значение слайдера,
    /// 2 - максимальное значение слайдера,
    /// 3 - текущая ширина элемента.</param>
    /// <param name="targetTypes">Тип целевого свойства привязки.</param>
    /// <param name="parameter">Дополнительный параметр преобразования.</param>
    /// <param name="culture">Культура для преобразования.</param>
    /// <returns>Рассчитанная ширина элемента UI, соответствующая значению слайдера.</returns>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }
  }
}
