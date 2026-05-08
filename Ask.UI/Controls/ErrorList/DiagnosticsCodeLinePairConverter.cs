using Ask.Core.Services.Errors.Models;
using System.Globalization;
using System.Windows.Data;

namespace Ask.UI.Controls.ErrorList
{
  /// <summary>
  /// Формирует строковое представление номеров строк 
  /// диагностического сообщения.
  /// </summary>
  /// <remarks>
  /// На основании данных <see cref="IDisplayIssue"/> возвращает:
  /// <list type="bullet">
  /// <item>
  /// <description>
  /// <c>"Source (Formatted)"</c>, если заданы оба номера строки.
  /// </description>
  /// </item>
  /// <item>
  /// <description>
  /// Только номер исходной строки, если задан только <c>SourceLineNumber</c>.
  /// </description>
  /// </item>
  /// <item>
  /// <description>
  /// Номер форматированной строки в скобках, 
  /// если задан только <c>FormattedLineNumber</c>.
  /// </description>
  /// </item>
  /// </list>
  /// Если номера строк отсутствуют или значение некорректно —
  /// возвращается <see cref="string.Empty"/>.
  /// </remarks>
  public class DiagnosticsCodeLinePairConverter : IValueConverter
  {
    /// <summary>
    /// Преобразует объект <see cref="IDisplayIssue"/> 
    /// в строковое представление номеров строк.
    /// </summary>
    /// <param name="value">
    /// Экземпляр <see cref="IDisplayIssue"/>, содержащий информацию о строках.
    /// </param>
    /// <param name="targetType">Тип целевого свойства (не используется).</param>
    /// <param name="parameter">Дополнительный параметр (не используется).</param>
    /// <param name="culture">Культура преобразования (не используется).</param>
    /// <returns>
    /// Строка с номерами строк либо <see cref="string.Empty"/>,
    /// если данные отсутствуют.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is not IDisplayIssue issue)
        return string.Empty;

      return BuildLineNumberText(issue);
    }

    /// <summary>
    /// Формирует строку отображения номеров строк.
    /// </summary>
    /// <param name="issue">
    /// Объект с информацией о номерах строк.
    /// </param>
    /// <returns>
    /// Строковое представление номеров строк.
    /// </returns>
    private static string BuildLineNumberText(IDisplayIssue issue)
    {
      bool hasSource = issue.SourceLineNumber > 0;
      bool hasFormatted = issue.FormattedLineNumber > 0;

      if (hasSource && hasFormatted)
        return $"{issue.SourceLineNumber} ({issue.FormattedLineNumber})";

      if (hasSource)
        return issue.SourceLineNumber.ToString();

      if (hasFormatted)
        return $"({issue.FormattedLineNumber})";

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
