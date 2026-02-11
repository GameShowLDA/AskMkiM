using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ask.UI.Shared.Converters.Math
{
  /// <summary>
  /// Выполняет умножение числового значения на коэффициент,
  /// переданный через <see cref="IValueConverter"/>.
  /// </summary>
  /// <remarks>
  /// Коэффициент передаётся через <paramref name="parameter"/>.
  /// Поддерживается строковое представление числа 
  /// в инвариантной культуре.
  /// 
  /// Пример использования в XAML:
  /// <code>
  /// Width="{Binding ActualWidth,
  ///         Converter={StaticResource MultiplyConverter},
  ///         ConverterParameter=0.5}"
  /// </code>
  /// 
  /// При некорректных входных данных возвращается 
  /// <see cref="Binding.DoNothing"/>.
  /// </remarks>
  public sealed class MultiplyConverter : IValueConverter
  {
    /// <summary>
    /// Умножает входное значение на коэффициент.
    /// </summary>
    /// <param name="value">
    /// Числовое значение (<see cref="double"/>).
    /// </param>
    /// <param name="targetType">
    /// Тип целевого свойства (ожидается <see cref="double"/>).
    /// </param>
    /// <param name="parameter">
    /// Коэффициент умножения (строка или число).
    /// </param>
    /// <param name="culture">Культура преобразования (не используется).</param>
    /// <returns>
    /// Результат умножения либо <see cref="Binding.DoNothing"/>
    /// при некорректных данных.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (!TryGetDouble(value, out double input))
        return Binding.DoNothing;

      if (!TryGetFactor(parameter, out double factor))
        return Binding.DoNothing;

      return input * factor;
    }

    /// <summary>
    /// Обратное преобразование не поддерживается.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Всегда генерируется, так как двусторонняя привязка не предусмотрена.
    /// </exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    /// <summary>
    /// Пытается безопасно извлечь числовое значение.
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

    /// <summary>
    /// Пытается извлечь коэффициент умножения.
    /// </summary>
    private static bool TryGetFactor(object parameter, out double factor)
    {
      factor = 0d;

      if (parameter == null)
        return false;

      if (parameter is double d)
      {
        factor = d;
        return true;
      }

      return double.TryParse(
          parameter.ToString(),
          NumberStyles.Any,
          CultureInfo.InvariantCulture,
          out factor);
    }
  }
}
