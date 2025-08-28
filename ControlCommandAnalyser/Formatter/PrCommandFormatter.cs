using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;

namespace ControlCommandAnalyser.Formatter
{
  internal class PrCommandFormatter : ICommandFormatter
  {
    public bool CanFormat(BaseCommandModel model) => model is PrCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      if (model is not PrCommandModel pr)
        yield break;

      // Первая строка: номер, мнемоника, нераспознанные параметры (если есть)
      var firstLine = $"{pr.CommandNumber} {pr.Mnemonic}";
      yield return firstLine;

      if (!string.IsNullOrWhiteSpace(pr.UnparsedParameters))
        yield return $"\t{pr.UnparsedParameters}";

      // Ключи команды
      if (pr.AlgorithmKey.Count > 0)
      {
        yield return $"\tКлючи команды: {string.Join(", ", pr.AlgorithmKey)}";
      }
      else
      {
        yield return $"\tКлючи команды не указаны.";
      }

      // Нижний порог сопротивления
      if (!string.IsNullOrWhiteSpace(pr.LowerLimitResistance))
      {
        yield return $"\tНижний порог сопротивления: {pr.LowerLimitResistance}";
      }


      // Верхний порог сопротивления
      if (!string.IsNullOrWhiteSpace(pr.HigherLimitResistance))
      {
        yield return $"\tВерхний порог сопротивления: {pr.HigherLimitResistance}";
      }
      else
      {
        yield return $"\tВерхний порог сопротивления не задан.";
      }

      if (pr.Points.Count > 0)
      {
        // Точки
        yield return $"\tЗаданные точки:";

        foreach (var point in pr.Points)
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
