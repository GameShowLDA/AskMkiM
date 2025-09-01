using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Chains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Formatter
{
  internal class IeCommandFormatter : ICommandFormatter
  {
    public bool CanFormat(BaseCommandModel model) => model is IeCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      if (model is not IeCommandModel ie)
        yield break;

      // Первая строка: номер, мнемоника, нераспознанные параметры (если есть)
      var firstLine = $"{ie.CommandNumber} {ie.Mnemonic}";
      yield return firstLine;

      if (!string.IsNullOrWhiteSpace(ie.UnparsedParameters))
        yield return $"\t{ie.UnparsedParameters}";

      // Ключи команды
      if (ie.AlgorithmKey.Count > 0)
      {
        yield return $"\tКлючи команды: {string.Join(", ", ie.AlgorithmKey)}";
      }
      else
      {
        yield return $"\tКлючи команды не указаны.";
      }

      if (string.IsNullOrWhiteSpace(ie.LowerLimitCapacity) && string.IsNullOrWhiteSpace(ie.HigherLimitCapacity))
      {
        yield return $"\tЭлектрическая емкость не задана!";
      }
      // Нижний порог электрической емкости
      else if (!string.IsNullOrWhiteSpace(ie.LowerLimitCapacity))
      {
        yield return $"\tНижний порог электрической емкости: {ie.LowerLimitCapacity}";
      }
      // Верхний порог электрической емкости
      else if (!string.IsNullOrWhiteSpace(ie.HigherLimitCapacity))
      {
        yield return $"\tВерхний порог электрической емкости: {ie.HigherLimitCapacity}";
      }

      if (ie.Scheme == null || ie.Scheme.IsEmpty())
      {
        yield return "\tТочки не заданы!";
        yield break;
      }

      yield return "\tЗаданные точки:";

      for (int ci = 0; ci < ie.Scheme.ChainModels.Count; ci++)
      {
        var chain = ie.Scheme.ChainModels[ci];
        if (chain?.ChainModels == null || chain.ChainModels.Count == 0) continue;
        else
        {
          yield return $"\t\tЦепь номер {ci + 1}:";
        }

        for (int pi = 0; pi < chain.ChainModels.Count; pi++)
        {
          var part = chain.ChainModels[pi];
          if (part?.PointModels == null || part.PointModels.Count == 0) continue;

          // первая часть цепи — отметим как начало (*), последующие части считаем "сообщёнными" (#)
          var status = (pi == 0) ? "*" : "#";

          foreach (var point in part.PointModels)
            yield return $"\t\t{status} {point}";
        }
      }


      yield return string.Empty;
    }
  }
}
