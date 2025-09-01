using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Chains;
using Utilities.Models;

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

      if (pr.Scheme == null || pr.Scheme.IsEmpty())
      {
        yield return "\tТочки не заданы!";
        yield break;
      }

      yield return "\tЗаданные точки:";

      for (int ci = 0; ci < pr.Scheme.ChainModels.Count; ci++)
      {
        var chain = pr.Scheme.ChainModels[ci];
        if (chain?.ChainModels == null || chain.ChainModels.Count == 0) continue;

        for (int pi = 0; pi < chain.ChainModels.Count; pi++)
        {
          var part = chain.ChainModels[pi];
          if (part?.PointModels == null || part.PointModels.Count == 0) continue;

          // первая часть цепи — отметим как начало (*), последующие части считаем "сообщёнными" (#)
          var status = (pi == 0) ? "*" : "#";

          foreach (var point in part.PointModels)
          {
            if (part.PointModels.IndexOf(point) == 0 && part.PointModels.Count > 1)
            {
              yield return $"\t\t{status} {point},";
            }
            else if (part.PointModels.IndexOf(point) == 0 && part.PointModels.Count == 1)
            {
              yield return $"\t\t{status} {point}";
            }
            else if (part.PointModels.IndexOf(point) != part.PointModels.Count - 1)
            {
              yield return $"\t\t  {point},";
            }
            else
            {
              yield return $"\t\t  {point}";
            }
          }
        }
      }

      yield return string.Empty;
    }
  }
}
