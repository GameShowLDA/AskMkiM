using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  internal class EhtCommandFormatter : ICommandFormatter
  {
    public bool CanFormat(BaseCommandModel model) => model is EhtCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      if (model is not EhtCommandModel eht)
        yield break;

      var firstLine = $"{eht.CommandNumber} {eht.Mnemonic}";
      yield return firstLine;

      if (!string.IsNullOrWhiteSpace(eht.UnparsedParameters))
        yield return $"\t{eht.UnparsedParameters}";

      // Ключи команды
      if (eht.AlgorithmKey.Count > 0)
      {
        yield return $"\tКлючи команды: {string.Join(", ", eht.AlgorithmKey)}";
      }
      else
      {
        yield return $"\tКлючи команды не указаны.";
      }

      // Нижний порог сопротивления
      if (!string.IsNullOrWhiteSpace(eht.LowerLimitResistanceSource))
      {
        yield return $"\tНижний порог сопротивления: {eht.LowerLimitResistanceSource}";
      }


      // Верхний порог сопротивления
      if (!string.IsNullOrWhiteSpace(eht.HigherLimitResistanceSource))
      {
        yield return $"\tВерхний порог сопротивления: {eht.HigherLimitResistanceSource}";
      }
      else
      {
        yield return $"\tВерхний порог сопротивления не задан.";
      }


      // Верхний порог сопротивления
      if (!string.IsNullOrWhiteSpace(eht.CabelResistanceSource))
      {
        yield return $"\tСопротивление проводов: {eht.CabelResistanceSource}";
      }
      else
      {
        yield return $"\tСопротивление проводов не задано.";
      }

      if (eht.Comment.Count > 0)
      {
        yield return $"\tКомметрии:";
        foreach (var line in eht.Comment)
        {
          var trimmed = line.Trim();
          if (!string.IsNullOrEmpty(trimmed))
            yield return $"\t\t{trimmed}";
        }
      }

      // Время
      if (!string.IsNullOrWhiteSpace(eht.TimeSource))
      {
        yield return $"\tВремя выдержки: {eht.TimeSource}";
      }
      else
      {
        yield return $"\tВремя выдержки не задано";
      }

      yield return "\tПроверяемые точки:";
      if (CommandsModel.GetRMModel() == null)
      {
        yield return "\tМодель РМ не задана!";
        yield break;
      }
      if (eht.Scheme == null || eht.Scheme.IsEmpty())
      {
        yield return "\t\tТочки не заданы!";
        yield break;
      }

      if (eht.Scheme.GroupModels.Count > 0)
      {
        for (int i = 0; i < eht.Scheme.GroupModels.Count; i++)
        {
          var pointsAll = eht.Scheme.GetPointsConnected(eht.Scheme.GroupModels[i]);
          if (pointsAll != null)
          {
            foreach (var points in pointsAll)
            {
              string str = string.Empty;
              str += $"\t\t{i + 1}. *";

              foreach (var point in points)
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
