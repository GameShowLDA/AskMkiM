using System.Globalization;
using System.Windows.Data;

namespace Ask.UI.Shared.Converters.Text
{
  /// <summary>
  /// Преобразует количество ошибок в строковое представление
  /// для отображения в заголовке UI.
  /// </summary>
  /// <remarks>
  /// Если входное значение является целым числом,
  /// возвращается его строковое представление.
  /// При некорректных данных возвращается "0".
  /// </remarks>
  public sealed class ErrorCountToHeaderConverter : IValueConverter
  {
    /// <summary>
    /// Преобразует количество ошибок в строку.
    /// </summary>
    /// <param name="value">
    /// Количество ошибок (<see cref="int"/>).
    /// </param>
    /// <param name="targetType">
    /// Тип целевого свойства (ожидается <see cref="string"/>).
    /// </param>
    /// <param name="parameter">Дополнительный параметр (не используется).</param>
    /// <param name="culture">
    /// Культура форматирования (не используется).
    /// </param>
    /// <returns>
    /// Строковое представление количества ошибок.
    /// При некорректном значении возвращается "0".
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is int count)
        return System.Math.Max(0, count).ToString(CultureInfo.InvariantCulture);

      return "0";
    }

    /// <summary>
    /// Обратное преобразование не поддерживается.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Всегда генерируется, так как двусторонняя привязка не предусмотрена.
    /// </exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
  }
}
