using Ask.Core.Services.Errors.Models;
using System.Globalization;
using System.Windows.Data;

namespace Ask.UI.Shared.Converters.Diagnostics
{
  /// <summary>
  /// Формирует строковое представление номеров строк предупреждения.
  /// </summary>
  /// <remarks>
  /// Возвращает:
  /// <list type="bullet">
  /// <item><description><c>"Source (Formatted)"</c>, если заданы оба номера.</description></item>
  /// <item><description>Только номер исходной строки, если задан только <c>SourceLineNumber</c>.</description></item>
  /// <item><description>Номер форматированной строки в скобках, если задан только <c>FormattedLineNumber</c>.</description></item>
  /// </list>
  /// Если номера строк отсутствуют или значение некорректно — 
  /// возвращается <see cref="string.Empty"/>.
  /// </remarks>
  public class WarningLinePairConverter : IValueConverter
  {
    /// <summary>
    /// Преобразует объект <see cref="WarningItem"/> 
    /// в строковое представление номеров строк.
    /// </summary>
    /// <param name="value">
    /// Экземпляр <see cref="WarningItem"/>, содержащий информацию о строках.
    /// </param>
    /// <param name="targetType">Тип целевого свойства (не используется).</param>
    /// <param name="parameter">Дополнительный параметр (не используется).</param>
    /// <param name="culture">Культура преобразования (не используется).</param>
    /// <returns>
    /// Строка с номерами строк либо <see cref="string.Empty"/>.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is not WarningItem warning)
        return string.Empty;

      return BuildLineNumberText(warning.SourceLineNumber, warning.FormattedLineNumber);
    }

    /// <summary>
    /// Формирует строку отображения номеров строк.
    /// </summary>
    /// <param name="sourceLine">Номер исходной строки.</param>
    /// <param name="formattedLine">Номер форматированной строки.</param>
    /// <returns>Строковое представление номеров строк.</returns>
    private static string BuildLineNumberText(int sourceLine, int formattedLine)
    {
      bool hasSource = sourceLine > 0;
      bool hasFormatted = formattedLine > 0;

      if (hasSource && hasFormatted)
        return $"{sourceLine} ({formattedLine})";

      if (hasSource)
        return sourceLine.ToString();

      if (hasFormatted)
        return $"({formattedLine})";

      return string.Empty;
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
