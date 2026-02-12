using System.Globalization;
using System.Windows.Data;

namespace Ask.UI.Shared.Converters.Text
{
  /// <summary>
  /// Возвращает символ перевода строки перед сообщением,
  /// если заголовок присутствует, а сообщение отсутствует.
  /// </summary>
  /// <remarks>
  /// Ожидает два входных значения:
  /// <list type="number">
  /// <item><description>Заголовок (<see cref="string"/>).</description></item>
  /// <item><description>Сообщение (<see cref="string"/>).</description></item>
  /// </list>
  /// 
  /// Возвращает перевод строки только в случае:
  /// <code>
  /// header != null/empty AND message == null/empty
  /// </code>
  /// Во всех остальных случаях возвращается <see cref="string.Empty"/>.
  /// </remarks>
  public sealed class MessageToLeadingNewLineConverter : IMultiValueConverter
  {
    /// <summary>
    /// Определяет необходимость добавления перевода строки.
    /// </summary>
    /// <param name="values">
    /// Массив входных значений (header, message).
    /// </param>
    /// <param name="targetType">
    /// Тип целевого свойства (ожидается <see cref="string"/>).
    /// </param>
    /// <param name="parameter">Дополнительный параметр (не используется).</param>
    /// <param name="culture">Культура преобразования (не используется).</param>
    /// <returns>
    /// <see cref="Environment.NewLine"/>, если требуется перенос строки;
    /// иначе <see cref="string.Empty"/>.
    /// </returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
      if (values is null || values.Length < 2)
        return string.Empty;

      string header = values[0] as string;
      string message = values[1] as string;

      bool hasHeader = !string.IsNullOrEmpty(header);
      bool hasMessage = !string.IsNullOrEmpty(message);

      if (hasHeader && !hasMessage)
        return Environment.NewLine;

      return string.Empty;
    }

    /// <summary>
    /// Обратное преобразование не поддерживается.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Всегда генерируется, так как двусторонняя привязка не предусмотрена.
    /// </exception>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
  }

}
