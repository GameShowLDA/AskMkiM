using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Chains;
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

      if (ks.Scheme == null || ks.Scheme.IsEmpty())
      {
        yield return "\tТочки не заданы!";
        yield break;
      }

      yield return "\tЗаданные точки:";

      for (int ci = 0; ci < ks.Scheme.GroupModels.Count; ci++)
      {
        var chain = ks.Scheme.GroupModels[ci];
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
