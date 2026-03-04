using Ask.Core.Services.Extensions;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  public class NeCommandFormatter : ICommandFormatter
  {
    public bool CanFormat(BaseCommandModel model) => model is NeCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      if (model is not NeCommandModel ne)
        yield break;

      // Первая строка: номер, мнемоника, нераспознанные параметры (если есть)
      var firstLine = $"{ne.CommandNumber} {ne.Mnemonic}";
      yield return firstLine;

      if (!string.IsNullOrWhiteSpace(ne.UnparsedParameters))
        yield return $"\t{ne.UnparsedParameters}";

      // Ключи команды
      if (ne.AlgorithmKey.Count > 0)
      {
        yield return $"\tКлючи команды: {string.Join(", ", ne.AlgorithmKey)}";
      }
      else
      {
        yield return $"\tКлючи команды не указаны.";
      }

      // TODO: заменить на точный измеритель в дальнейшем
      var meter = new DataBaseConfiguration.Services.Device.FastMeterServices().GetAll().FirstOrDefault();
      //var minResistance = Measurement.MeasurementTypeCommand.PR.GetDisplayInfo().LowerLimit;
      if (meter == null)
      {
        LogError($"Не найден измеритель.");
      }
      else
      {
        yield return $"\tИспользуемый измеритель: {meter.Name}";
      }

      if (ne.Comment.Count > 0)
      {
        yield return $"\tКомментарии:";
        foreach (var line in ne.Comment)
        {
          var trimmed = line.Trim();
          if (!string.IsNullOrEmpty(trimmed))
            yield return $"\t\t{trimmed}";
        }
      }

      if (CommandsModel.GetRMModel() == null)
      {
        yield return "\tМодель РМ не задана!";
        yield break;
      }
      if (ne.Scheme == null || ne.Scheme.IsEmpty())
      {
        yield return "\t\tТочки не заданы!";
        yield break;
      }

      yield return "\tПроверка диода в прямом направлении:";

      // Нижний порог сопротивления
      if (!string.IsNullOrWhiteSpace(ne.LowerLimitVoltageSource))
      {
        yield return $"\t\tНижний порог напряжения: {ne.LowerLimitVoltageSource}";
      }
      else
      {
        yield return $"\t\tНижний порог напряжения не задан!";
      }

      // Верхний порог сопротивления
      if (!string.IsNullOrWhiteSpace(ne.HigherLimitVoltageSource))
      {
        yield return $"\t\tВерхний порог напряжения: {ne.HigherLimitVoltageSource}";
      }
      else
      {
        yield return $"\t\tВерхний порог напряжения не задан!";
      }

      if (!ne.AlgorithmKey.Contains(AlgorithmKey.Н.ToString()))
      {
        yield return "\tПроверка диода в обратном направлении:";

        // Напряжение
        if (!string.IsNullOrWhiteSpace(ne.VoltageSource))
        {
          yield return $"\t\tНапряжение: {ne.VoltageSource}";
        }
        else
        {
          yield return $"\t\tНапряжение не задано!";
        }
      }

      if (ne.Scheme.GroupModels.Count > 0)
      {
        yield return "\t\tСписок проверяемых точек:";
        if (ne.ElementEnablingType.Count > 0)
        {
          var j = 1;
          for (int i = 0; i < ne.Scheme.GroupModels.Count; i++)
          {
            var groupChains = ne.Scheme.GetPointsConnected(ne.Scheme.GroupModels[i]);
            if (groupChains != null)
            {
              foreach (var chains in groupChains.ChainModels)
              {
                string str = string.Empty;
                str += $"\t\t\t{j}. *{ne.ElementEnablingType[j - 1].Item2.GetDescription()}";
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
      }


      yield return string.Empty;
    }
  }
}
