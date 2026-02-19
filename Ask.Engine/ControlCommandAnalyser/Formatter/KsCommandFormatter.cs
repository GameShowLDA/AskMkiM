using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
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
      if (!string.IsNullOrWhiteSpace(ks.LowerLimitResistanceSource))
      {
        yield return $"\tНижний порог сопротивления: {ks.LowerLimitResistanceSource}";
      }
      else
      {
        yield return $"\tНижний порог сопротивления не задан.";
      }


      // Верхний порог сопротивления
      if (!string.IsNullOrWhiteSpace(ks.HigherLimitResistanceSource))
      {
        yield return $"\tВерхний порог сопротивления: {ks.HigherLimitResistanceSource}";
      }
      else
      {
        yield return $"\tВерхний порог сопротивления не задан.";
      }

      if (ks.Comment.Count > 0)
      {
        yield return $"\tКомметрии:";
        foreach (var line in ks.Comment)
        {
          var trimmed = line.Trim();
          if (!string.IsNullOrEmpty(trimmed))
            yield return $"\t\t{trimmed}";
        }
      }

      yield return "\tПроверяемые точки:";
      if (CommandsModel.GetRMModel() == null)
      {
        yield return "\t\tМодель РМ не задана!";
        yield break;
      }
      if (ks.Scheme == null || ks.Scheme.IsEmpty())
      {
        yield return "\t\tТочки не заданы!";
        yield break;
      }

      if (ks.Scheme.GroupModels.Count > 0)
      {
        for (int i = 0; i < ks.Scheme.GroupModels.Count; i++)
        {
          var groupChains = ks.Scheme.GetPointsConnected(ks.Scheme.GroupModels[i]);
          if (groupChains != null)
          {
            foreach (var chains in groupChains.ChainModels)
            {
              string str = string.Empty;
              str += $"\t\t{i + 1}. *";

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
