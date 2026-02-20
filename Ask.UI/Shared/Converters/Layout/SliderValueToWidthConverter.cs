using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Ask.UI.Shared.Converters.Layout
{
  using System;
  using System.Globalization;
  using System.Windows;
  using System.Windows.Data;

  /// <summary>
  /// Преобразует значение <see cref="Slider"/> в пропорциональную ширину элемента UI.
  /// </summary>
  /// <remarks>
  /// Ожидает четыре входных значения в следующем порядке:
  /// <list type="number">
  /// <item><description>Текущее значение слайдера (<see cref="double"/>).</description></item>
  /// <item><description>Минимальное значение слайдера (<see cref="double"/>).</description></item>
  /// <item><description>Максимальное значение слайдера (<see cref="double"/>).</description></item>
  /// <item><description>Фактическая ширина контейнера (<see cref="double"/>).</description></item>
  /// </list>
  /// 
  /// Ширина вычисляется по формуле:
  /// <code>
  /// normalized = (value - min) / (max - min)
  /// result = normalized * actualWidth
  /// </code>
  /// 
  /// Значение нормализуется в диапазон [0;1].
  /// При некорректных входных данных возвращается 0.
  /// </remarks>
  public sealed class SliderValueToWidthConverter : IMultiValueConverter
  {
    /// <summary>
    /// Преобразует значение слайдера в пропорциональную ширину.
    /// </summary>
    /// <param name="values">
    /// Массив входных значений (value, min, max, actualWidth).
    /// </param>
    /// <param name="targetType">
    /// Тип целевого свойства (ожидается <see cref="double"/>).
    /// </param>
    /// <param name="parameter">Дополнительный параметр (не используется).</param>
    /// <param name="culture">Культура преобразования (не используется).</param>
    /// <returns>
    /// Рассчитанная ширина элемента либо 0 при некорректных данных.
    /// </returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (!TryExtractValues(values, out double value, out double min, out double max, out double actualWidth))
        return 0d;

      double range = max - min;
      if (range <= 0)
        return 0d;

      double normalized = (value - min) / range;
      normalized = Math.Max(0d, Math.Min(1d, normalized));

      return normalized * actualWidth;
    }

    /// <summary>
    /// Обратное преобразование не поддерживается.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Всегда генерируется, так как двусторонняя привязка не предусмотрена.
    /// </exception>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    /// <summary>
    /// Пытается безопасно извлечь входные значения из массива привязки.
    /// </summary>
    private static bool TryExtractValues(
        object[] values,
        out double value,
        out double min,
        out double max,
        out double actualWidth)
    {
      value = min = max = actualWidth = 0d;

      if (values is null || values.Length < 4)
        return false;

      if (!TryGetDouble(values[0], out value)) return false;
      if (!TryGetDouble(values[1], out min)) return false;
      if (!TryGetDouble(values[2], out max)) return false;
      if (!TryGetDouble(values[3], out actualWidth)) return false;

      return true;
    }

    /// <summary>
    /// Безопасно преобразует объект в <see cref="double"/>.
    /// </summary>
    private static bool TryGetDouble(object input, out double result)
    {
      result = 0d;

      if (input == null || input == DependencyProperty.UnsetValue)
        return false;

      if (input is double d && !double.IsNaN(d) && !double.IsInfinity(d))
      {
        result = d;
        return true;
      }

      return false;
    }
  }

}
