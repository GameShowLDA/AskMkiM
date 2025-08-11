using ControlCommandAnalyser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Formatter
{
  public class KsCommandFormatter : ICommandFormatter
  {
    public bool CanFormat(BaseCommandModel model) => model is KsCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      if (model is not KsCommandModel ks)
        yield break;

      // Первая строка: номер, мнемоника, нераспознанные параметры (если есть)
      var firstLine = $"{ks.CommandNumber} {ks.Mnemonic}";
      yield return firstLine;

      if (!string.IsNullOrWhiteSpace(ks.UnparsedParameters))
        yield return $"\t{ks.UnparsedParameters}";

      // Ключи команды
      if (ks.AlgorithmKey.Count > 0)
      {
        yield return $"\tКлючи команды: {string.Join(", ", ks.AlgorithmKey)}";
      }
      else
      {
        yield return $"\tКлючи команды не указаны.";
      }

      // Нижний порог сопротивления
      if (!string.IsNullOrWhiteSpace(ks.LowerLimitResistance))
      {
        yield return $"\tНижний порог сопротивления: {ks.LowerLimitResistance}";
      }
      else
      {
        yield return $"\tНижний порог сопротивления не задан.";
      }
      

      // Верхний порог сопротивления
      if (!string.IsNullOrWhiteSpace(ks.HigherLimitResistance))
      {
        yield return $"\tВерхний порог сопротивления: {ks.HigherLimitResistance}";
      }
      else
      {
        yield return $"\tВерхний порог сопротивления не задан.";
      }

      // Время
      if (!string.IsNullOrWhiteSpace(ks.Time))
      {
        yield return $"\tВремя выполнения: {ks.Time}";
      }
      else
      {
        yield return $"\tВремя выполнения не задано.";
      }

      if (ks.Points.Count > 0)
      {
        // Точки
        yield return $"\tЗаданные точки:";

        foreach (var point in ks.Points)
          yield return $"\t\t{point.Item1},{point.Item2}";
      }
      else
      {
        yield return $"\tТочки не заданы!";
      }

      yield return string.Empty;
    }
  }
}
