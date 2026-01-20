using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
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
      if (!string.IsNullOrWhiteSpace(pr.LowerLimitResistanceSource))
      {
        yield return $"\tНижний порог сопротивления: {pr.LowerLimitResistanceSource}";
      }


      // Верхний порог сопротивления
      if (!string.IsNullOrWhiteSpace(pr.HigherLimitResistanceSource))
      {
        yield return $"\tВерхний порог сопротивления: {pr.HigherLimitResistanceSource}";
      }
      else
      {
        yield return $"\tВерхний порог сопротивления не задан.";
      }

      if (pr.Comment.Count > 0)
      {
        yield return $"\tКомметрии:";
        foreach (var line in pr.Comment)
        {
          var trimmed = line.Trim();
          if (!string.IsNullOrEmpty(trimmed))
            yield return $"\t\t{trimmed}";
        }
      }

      // Время
      if (!string.IsNullOrWhiteSpace(pr.TimeSource))
      {
        yield return $"\tВремя выполнения: {pr.TimeSource}";
      }
      else
      {
        yield return $"\tВремя выполнения не задано.";
      }

      yield return "\tЗаданные точки:";
      if (CommandsModel.GetRMModel() == null)
      {
        yield return "\tМодель РМ не задана!";
        yield break;
      }
      if (pr.Scheme == null || pr.Scheme.IsEmpty())
      {
        yield return "\t\tТочки не заданы!";
        yield break;
      }

      if (pr.Scheme.GroupModels.Count > 0 && !pr.AlgorithmKey.Contains(AlgorithmKey.ЗР.ToString()))
      {
        yield return "\t\tРазобщенные точки:";
        for (int i = 0; i < pr.Scheme.GroupModels.Count; i++)
        {
          var points = pr.Scheme.GetPointsDisconnected(pr.Scheme.GroupModels[i]);
          if (points != null)
          {
            string str = string.Empty;
            str += $"\t\t{i + 1}. *";

            foreach (var point in points.PointModels)
            {
              str += $"{point.Mnemonic}[{point}]#";
            }
            yield return str.Remove(str.Length - 1);
          }
        }
      }


      if (pr.Scheme.GroupModels.Count > 0 && !pr.AlgorithmKey.Contains(AlgorithmKey.ЗС.ToString()))
      {
        yield return "\tСообщенные точки:";

        var j = 1;
        for (int i = 0; i < pr.Scheme.GroupModels.Count; i++)
        {
          var groupChains = pr.Scheme.GetPointsConnected(pr.Scheme.GroupModels[i]);
          if (groupChains != null)
          {
            foreach (var chains in groupChains.ChainModels)
            {
              string str = string.Empty;
              str += $"\t\t{j}. *";
              j++;
              foreach (var point in chains.PointModels)
              {
                str += $"{point.Mnemonic}[{point}],";
              }
              yield return str.Remove(str.Length - 1);
            }
          }
        }
      }

      yield return string.Empty;
    }
  }
}
