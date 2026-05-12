using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  /// <summary>
  /// Форматтер для команды СИ (сопротивление изоляции).
  /// </summary>
  public class SiCommandFormatter : ICommandFormatter
  {
    public bool CanFormat(BaseCommandModel model) => model is SiCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      if (model is not SiCommandModel si)
        yield break;

      // Первая строка: номер, мнемоника, нераспознанные параметры (если есть)
      var firstLine = $"{si.CommandNumber} {si.Mnemonic}";
      yield return firstLine;

      if (!string.IsNullOrWhiteSpace(si.UnparsedParameters))
        yield return $"\t{si.UnparsedParameters}";

      // Ключи команды
      if (si.AlgorithmKey.Count > 0)
      {
        yield return $"\tКлючи команды: {string.Join(", ", si.AlgorithmKey)}";
      }
      else
      {
        yield return $"\tКлючи команды не указаны.";
      }

      // Напряжение
      if (!string.IsNullOrWhiteSpace(si.VoltageSource))
      {
        yield return $"\tНапряжение: {si.VoltageSource}";
      }
      else
      {
        yield return $"\tНапряжение не задано!";
      }

      // Время
      if (!string.IsNullOrWhiteSpace(si.TimeSource))
      {
        yield return $"\tВремя ожидания НОРМЫ: {si.TimeSource}";
      }
      else
      {
        yield return $"\tВремя ожидания НОРМЫ не задано!";
      }

      // Сопротивление
      if (!string.IsNullOrWhiteSpace(si.ResistanceSource))
      {
        yield return $"\tСопротивление: {si.ResistanceSource}";
      }
      else
      {
        yield return $"\tСопротивление не задано!";
      }
      if (si.Comment.Count > 0)
      {
        yield return $"\tКомментарии:";
        foreach (var line in si.Comment)
        {
          var trimmed = line.Trim();
          if (!string.IsNullOrEmpty(trimmed))
            yield return $"\t\t{trimmed}";
        }
      }
      yield return "\tРазобщенные точки:";
      if (CommandsModel.GetRMModel() == null)
      {
        yield return "\t\tМодель РМ не задана!";
        yield break;
      }
      else if (si.Scheme == null || si.Scheme.IsEmpty())
      {
        yield return "\t\tТочки не заданы!";
        yield break;
      }

      for (int i = 0; i < si.Scheme.GroupModels.Count; i++)
      {
        var points = si.Scheme.GetPointsDisconnected(si.Scheme.GroupModels[i]);
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

      yield return string.Empty;
    }
  }
}
