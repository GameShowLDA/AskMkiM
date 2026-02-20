using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
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

      if (string.IsNullOrWhiteSpace(ie.LowerLimitCapacitySource) && string.IsNullOrWhiteSpace(ie.HigherLimitCapacitySource))
      {
        yield return $"\tЭлектрическая емкость не задана!";
      }
      // Нижний порог электрической емкости
      else if (!string.IsNullOrWhiteSpace(ie.LowerLimitCapacitySource))
      {
        yield return $"\tНижний порог электрической емкости: {ie.LowerLimitCapacitySource}";
        // Верхний порог электрической емкости
        if (!string.IsNullOrWhiteSpace(ie.HigherLimitCapacitySource))
        {
          yield return $"\tВерхний порог электрической емкости: {ie.HigherLimitCapacitySource}";
        }
      }

      if (ie.Comment.Count > 0)
      {
        yield return $"\tКомметрии:";
        foreach (var line in ie.Comment)
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
      if (ie.Scheme == null || ie.Scheme.IsEmpty())
      {
        yield return "\t\tТочки не заданы!";
        yield break;
      }

      if (ie.Scheme.GroupModels.Count > 0)
      {
        for (int i = 0; i < ie.Scheme.GroupModels.Count; i++)
        {
          var groupChains = ie.Scheme.GetPointsConnected(ie.Scheme.GroupModels[i]);
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
