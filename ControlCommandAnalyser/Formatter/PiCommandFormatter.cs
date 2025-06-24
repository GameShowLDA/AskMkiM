using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;

namespace ControlCommandAnalyser.Formatter
{
  /// <summary>
  /// Форматтер для команды ПИ (пробой изоляции).
  /// </summary>
  public class PiCommandFormatter : ICommandFormatter
  {
    public bool CanFormat(BaseCommandModel model) => model is PiCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      if (model is not PiCommandModel pi)
        yield break;

      // Первая строка: номер, мнемоника, нераспознанные параметры (если есть)
      var firstLine = $"{pi.CommandNumber} {pi.Mnemonic}";
      yield return firstLine;

      if (!string.IsNullOrWhiteSpace(pi.UnparsedParameters))
        yield return $"\t{pi.UnparsedParameters}";

      // Напряжение
      if (!string.IsNullOrWhiteSpace(pi.Voltage))
        yield return $"\tНапряжение: {pi.Voltage}";
      else
        yield return $"\tНапряжение не задано!";

      // Пороговое сопротивление
      if (!string.IsNullOrWhiteSpace(pi.ThresholdResistance))
        yield return $"\tПороговое сопротивление: {pi.ThresholdResistance}";
      else
        yield return $"\tПороговое сопротивление не задано!";

      // Время
      if (!string.IsNullOrWhiteSpace(pi.Time))
        yield return $"\tВремя выполнения: {pi.Time}";
      else
        yield return $"\tВремя выполнения не задано!";

      if (pi.Points.Count > 0)
      {
        yield return $"\tЗаданные точки:";
        foreach (var point in pi.Points)
          yield return $"\t\t{point}";
      }
      else
      {
        yield return $"\tТочки не заданы!";
      }

      yield return string.Empty;
    }
  }
}
