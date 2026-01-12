using System.Globalization;
using System.Windows.Data;

namespace UI.Controls.EmptyWorkspace
{
  /// <summary>
  /// Преобразует DateTime в строку по заданному формату.
  /// Параметр: "формат" или "формат|TitleCase" для капитализации (например: "dddd|TitleCase").
  /// </summary>
  public class DateTimeToStringConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      if (value is not DateTime dt) return string.Empty;

      var param = parameter as string ?? "G";
      var parts = param.Split('|', StringSplitOptions.RemoveEmptyEntries);
      var format = parts.Length > 0 ? parts[0] : "G";
      var titleCase = parts.Length > 1 && parts[1].Equals("TitleCase", StringComparison.OrdinalIgnoreCase);

      // русская локаль для правильных месяцев/дней недели
      var ru = culture?.Name?.StartsWith("ru", StringComparison.OrdinalIgnoreCase) == true
                 ? culture
                 : new CultureInfo("ru-RU");

      var text = dt.ToString(format, ru);

      if (titleCase)
      {
        // деликатно капитализируем первую букву
        if (!string.IsNullOrEmpty(text))
          text = char.ToUpper(text[0], ru) + (text.Length > 1 ? text.Substring(1) : string.Empty);
      }

      return text;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
      throw new NotSupportedException();
  }
}
