using System.Globalization;
using System.Windows.Data;

namespace Ask.UI.Shared.Converters.DateTime
{
  /// <summary>
  /// Преобразует DateTime в строку по заданному формату.
  /// Параметр: "формат" или "формат|TitleCase" для капитализации (например: "dddd|TitleCase").
  /// </summary>
  public class DateTimeToStringConverter : IValueConverter
  {
    /// <summary>
    /// Преобразует значение <see cref="DateTime"/> в строку
    /// с учетом формата, параметров и русской локали.
    /// </summary>
    /// <param name="value">Значение для преобразования.</param>
    /// <param name="targetType">Тип результата (не используется).</param>
    /// <param name="parameter">
    /// Строка параметров в формате "Формат|TitleCase".
    /// </param>
    /// <param name="culture">Культура форматирования.</param>
    /// <returns>
    /// Отформатированная строка даты либо пустая строка,
    /// если входное значение не является <see cref="DateTime"/>.
    /// </returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (!TryExtractDate(value, out var date))
        return string.Empty;

      var (format, titleCase) = ParseParameter(parameter);
      var effectiveCulture = ResolveCulture(culture);

      var text = FormatDate(date, format, effectiveCulture);

      return titleCase
          ? ApplyTitleCase(text, effectiveCulture)
          : text;
    }

    /// <summary>
    /// Метод не реализован, так как двустороннее преобразование не требуется.
    /// </summary>
    /// <param name="value">Значение для преобразования назад.</param>
    /// <param name="targetTypes">Массив типов для преобразования.</param>
    /// <param name="parameter">Параметр (не используется).</param>
    /// <param name="culture">Культура (не используется).</param>
    /// <returns>Исключение <see cref="NotImplementedException"/>, так как преобразование назад не поддерживается.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();

    /// <summary>
    /// Пытается извлечь значение <see cref="DateTime"/> из входного объекта.
    /// </summary>
    /// <param name="value">Объект для проверки.</param>
    /// <param name="date">Извлеченная дата при успешном преобразовании.</param>
    /// <returns>
    /// <c>true</c>, если значение является <see cref="DateTime"/>; иначе <c>false</c>.
    /// </returns>
    private static bool TryExtractDate(object value, out System.DateTime date)
    {
      if (value is System.DateTime dt)
      {
        date = dt;
        return true;
      }

      date = default;
      return false;
    }

    /// <summary>
    /// Разбирает строку параметров форматирования.
    /// </summary>
    /// <param name="parameter">
    /// Строка вида "Формат|TitleCase".
    /// </param>
    /// <returns>
    /// Кортеж, содержащий строку формата и признак
    /// необходимости капитализации первой буквы.
    /// </returns>
    private static (string format, bool titleCase) ParseParameter(object parameter)
    {
      var param = parameter as string ?? "G";
      var parts = param.Split('|', StringSplitOptions.RemoveEmptyEntries);

      var format = parts.Length > 0 ? parts[0] : "G";
      var titleCase = parts.Length > 1 &&
                      parts[1].Equals("TitleCase", StringComparison.OrdinalIgnoreCase);

      return (format, titleCase);
    }

    /// <summary>
    /// Определяет культуру форматирования.
    /// Если переданная культура не русская,
    /// используется культура "ru-RU".
    /// </summary>
    /// <param name="culture">Исходная культура.</param>
    /// <returns>
    /// Культура для форматирования даты.
    /// </returns>
    private static CultureInfo ResolveCulture(CultureInfo culture)
    {
      if (culture?.Name?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true)
        return culture;

      return new CultureInfo("ru-RU");
    }

    /// <summary>
    /// Форматирует дату в строковое представление
    /// с использованием заданной культуры.
    /// </summary>
    /// <param name="date">Дата для форматирования.</param>
    /// <param name="format">Строка формата.</param>
    /// <param name="culture">Культура форматирования.</param>
    /// <returns>
    /// Отформатированная строка даты.
    /// </returns>
    private static string FormatDate(System.DateTime date, string format, CultureInfo culture)
    {
      return date.ToString(format, culture);
    }

    /// <summary>
    /// Выполняет капитализацию первой буквы строки
    /// с учетом указанной культуры.
    /// </summary>
    /// <param name="text">Исходный текст.</param>
    /// <param name="culture">Культура для преобразования регистра.</param>
    /// <returns>
    /// Строка с заглавной первой буквой.
    /// </returns>
    private static string ApplyTitleCase(string text, CultureInfo culture)
    {
      if (string.IsNullOrEmpty(text))
        return text;

      return char.ToUpper(text[0], culture) +
             (text.Length > 1 ? text.Substring(1) : string.Empty);
    }
  }
}
