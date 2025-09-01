using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Chains;

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

      if (pi.Scheme == null || pi.Scheme.IsEmpty())
      {
        yield return "\tТочки не заданы!";
        yield break;
      }

      yield return "\tЗаданные точки:";

      for (int ci = 0; ci < pi.Scheme.ChainModels.Count; ci++)
      {
        var chain = pi.Scheme.ChainModels[ci];
        if (chain?.ChainModels == null || chain.ChainModels.Count == 0) continue;
        else
        {
          yield return $"\t\tЦепь номер {ci + 1}:";
        }

        for (int i = 0; i < chain.ChainModels.Count; i++)
        {
          var part = chain.ChainModels[i];
          if (part?.PointModels == null || part.PointModels.Count == 0) continue;

          // первая часть цепи — отметим как начало (*), последующие части считаем "сообщёнными" (#)
          var status = (i == 0) ? "*" : "#";

          foreach (var point in part.PointModels)
            yield return $"\t\t{status} {point}";
        }
      }

      yield return string.Empty;
    }
  }
}
