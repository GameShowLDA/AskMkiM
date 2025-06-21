using System.Collections.Generic;
using ControlCommandAnalyser.Model;

namespace ControlCommandAnalyser.Formatter
{
  /// <summary>
  /// Форматтер для команды СИ (сопротивление изоляции).
  /// </summary>
  public class SiCommandFormatter : ICommandFormatter
  {
    public bool CanFormat(BaseCommandModel model) => model is SiCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      if (model is not SiCommandModel si)
        yield break;

      // Первая строка: номер, мнемоника, нераспознанные параметры (если есть)
      var firstLine = $"{si.CommandNumber} {si.Mnemonic}";
      if (!string.IsNullOrWhiteSpace(si.UnparsedParameters))
        firstLine += $" {si.UnparsedParameters}";
      yield return firstLine;

      // Напряжение
      if (!string.IsNullOrWhiteSpace(si.Voltage))
        yield return $"\t{si.Voltage}";

      // Время
      if (!string.IsNullOrWhiteSpace(si.Time))
        yield return $"\t{si.Time}";

      // Сопротивление
      if (!string.IsNullOrWhiteSpace(si.Resistance))
        yield return $"\t{si.Resistance}";

      // Точки
      foreach (var point in si.Points)
        yield return $"\t{point}";

      yield return string.Empty;
    }
  }
}
