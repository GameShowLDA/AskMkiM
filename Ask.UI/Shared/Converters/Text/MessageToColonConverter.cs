using System.Globalization;
using System.Windows.Data;

namespace Ask.UI.Shared.Converters.Text
{
  /// <summary>
  /// Возвращает символ двоеточия, если переданная строка сообщения не пустая.
  /// </summary>
  /// <remarks>
  /// Используется для условного добавления разделителя
  /// после заголовка или имени.
  /// Если сообщение отсутствует или пустое,
  /// возвращается <see cref="string.Empty"/>.
  /// </remarks>
  public sealed class MessageToColonConverter : IValueConverter
  {
    /// <summary>
    /// Преобразует строку сообщения в символ ":" при наличии значения.
    /// </summary>
    /// <param name="value">
    /// Строка сообщения (<see cref="string"/>).
    /// </param>
    /// <param name="targetType">
    /// Тип целевого свойства (ожидается <see cref="string"/>).
    /// </param>
    /// <param name="parameter">Дополнительный параметр (не используется).</param>
    /// <param name="culture">Культура преобразования (не используется).</param>
    /// <returns>
    /// ":" если сообщение не пустое; иначе <see cref="string.Empty"/>.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is string message && !string.IsNullOrEmpty(message))
        return ":";

      return string.Empty;
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
