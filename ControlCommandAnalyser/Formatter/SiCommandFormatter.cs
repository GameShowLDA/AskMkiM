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
      {
        yield return $"\tНапряжение: {si.Voltage}";
      }
      else
      {
        yield return $"\tНапряжение не задано!";
      }

      // Время
      if (!string.IsNullOrWhiteSpace(si.Time))
      {
        yield return $"\tВремя выполнения: {si.Time}";
      }
      else
      {
        yield return $"\tВремя выполнения не задано!";
      }

      // Сопротивление
      if (!string.IsNullOrWhiteSpace(si.Resistance))
      {
        yield return $"\tСопротивление: {si.Resistance}";
      }
      else
      {
        yield return $"\tСопротивление не задано!";
      }

      if (si.Points.Count > 0)
      {
        // Точки
        yield return $"\tЗаданные точки:";

        foreach (var point in si.Points)
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
