using System.Globalization;
using System.Windows.Data;

namespace Ask.UI.Shared.Converters.DateTime
{
  /// <summary>
  /// Возвращает строковый разделитель времени, 
  /// если переданное значение не пустое.
  /// </summary>
  /// <remarks>
  /// Если входная строка содержит значение времени, 
  /// возвращается символ разделителя (<c>"|"</c>).
  /// В противном случае возвращается пустая строка.
  /// </remarks>
  public class TimeToSeparatorConverter : IValueConverter
  {
    /// <summary>
    /// Преобразует строковое представление времени 
    /// в символ-разделитель.
    /// </summary>
    /// <param name="value">
    /// Строковое значение времени.
    /// </param>
    /// <param name="targetType">Тип целевого свойства (не используется).</param>
    /// <param name="parameter">Дополнительный параметр (не используется).</param>
    /// <param name="culture">Культура преобразования (не используется).</param>
    /// <returns>
    /// Строка <c>"|"</c>, если значение не пустое; 
    /// иначе <see cref="string.Empty"/>.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      var time = value as string;
      return string.IsNullOrEmpty(time) ? string.Empty : "|";
    }

    /// <summary>
    /// Обратное преобразование не поддерживается.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Всегда генерируется, так как двустороннее преобразование не предусмотрено.
    /// </exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
  }
}
