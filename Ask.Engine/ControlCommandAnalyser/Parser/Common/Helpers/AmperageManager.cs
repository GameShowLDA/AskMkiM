using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers
{
  /// <summary>
  /// Вспомогательный класс для разбора значения силы тока.
  /// Преобразует строковое значение в числовое и возвращает его вместе с единицей измерения.
  /// </summary>
  internal class AmperageManager
  {
    /// <summary>
    /// Парсит строковое представление значения тока.
    /// </summary>
    /// <param name="raw">Сырое значение (строка).</param>
    /// <param name="unit">Единица измерения.</param>
    /// <returns>
    /// Кортеж: числовое значение тока (если удалось распарсить) и единица измерения.
    /// </returns>
    public static (double? value, string? unit) Parse(string raw, string unit)
    {
      if (string.IsNullOrWhiteSpace(raw))
        return (null, unit);

      return (CommonParameterParser.ParseToDouble(raw), unit);
    }
  }
}
