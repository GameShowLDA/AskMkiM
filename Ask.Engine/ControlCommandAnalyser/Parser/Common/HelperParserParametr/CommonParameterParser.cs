using System.Globalization;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr
{
  /// <summary>
  /// Предоставляет методы для парсинга общих параметров команд: напряжения, сопротивления и времени.
  /// </summary>
  public static class CommonParameterParser
  {
    /// <summary>
    /// Св-во управления парсингом ёмкостью.
    /// </summary>
    public static CapacityParser CapacityParser => new CapacityParser();

    /// <summary>
    /// Св-во управления парсингом cопротивления.
    /// </summary>
    public static ResistanceParser ResistanceParser => new ResistanceParser();

    /// <summary>
    /// Св-во управления парсингом напряжения.
    /// </summary>
    public static VoltageParser VoltageParser => new VoltageParser();

    /// <summary>
    /// Св-во управления парсингом времени.
    /// </summary>
    public static TimeParser TimeParser => new TimeParser();

    /// <summary>
    /// Св-во управления парсингом времени.
    /// </summary>
    public static AmperageParser AmperageParser => new AmperageParser();

    /// <summary>
    /// Преобразует строку в число double.
    /// Поддерживает как точку, так и запятую в качестве десятичного разделителя.
    /// </summary>
    /// <param name="input">Входная строка, например "49", "49.2", "49,2".</param>
    /// <returns>Значение double.</returns>
    /// <exception cref="FormatException">Если строка не является числом.</exception>
    public static double ParseToDouble(string input)
    {
      if (string.IsNullOrWhiteSpace(input))
        throw new FormatException("Входная строка пуста.");

      string normalized = input.Replace(',', '.');

      if (double.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
      {
        return result;
      }

      throw new FormatException($"Не удалось преобразовать \"{input}\" в число.");
    }
  }
}
