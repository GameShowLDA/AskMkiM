using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Chains;

namespace Ask.Engine.ControlCommandAnalyser.Formatter
{
  /// <summary>
  /// Форматтер для команды ПИ (пробой изоляции).
  /// </summary>
  public class PiCommandFormatter : ICommandFormatter
  {
    public bool CanFormat(BaseCommandModel model) => model is PiCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      if (model is not PiCommandModel pi)
        yield break;

      // Первая строка: номер, мнемоника, нераспознанные параметры (если есть)
      var firstLine = $"{pi.CommandNumber} {pi.Mnemonic}";
      yield return firstLine;

      if (!string.IsNullOrWhiteSpace(pi.UnparsedParameters))
        yield return $"\t{pi.UnparsedParameters}";

      var si = pi.SiCommand;

      if (si != null)
      {

        yield return $"\tПараметры команды СИ:";
        // Ключи команды СИ
        if (si.AlgorithmKey.Count > 0)
        {
          yield return $"\t\tКлючи команды: {string.Join(", ", si.AlgorithmKey)}";
        }
        else
        {
          yield return $"\t\tКлючи команды СИ не указаны.";
        }

        // Напряжение
        if (!string.IsNullOrWhiteSpace(si.VoltageSource))
        {
          yield return $"\t\tНапряжение: {si.VoltageSource}";
        }
        else
        {
          yield return $"\t\tНапряжение не задано!";
        }

        // Время
        if (!string.IsNullOrWhiteSpace(si.TimeSource))
        {
          yield return $"\t\tВремя выполнения: {si.TimeSource}";
        }
        else
        {
          yield return $"\t\tВремя выполнения не задано!";
        }

        // Сопротивление
        if (!string.IsNullOrWhiteSpace(si.ResistanceSource))
        {
          yield return $"\t\tСопротивление: {si.ResistanceSource}";
        }
        else
        {
          yield return $"\t\tСопротивление не задано!";
        }

        yield return $"\tПараметры команды ПИ:";
        // Ключи команды ПИ
        if (pi.AlgorithmKey.Count > 0)
        {
          yield return $"\tКлючи команды: {string.Join(", ", pi.AlgorithmKey)}";
        }
        else
        {
          yield return $"\tКлючи команды не указаны.";
        }

        // Напряжение
        if (!string.IsNullOrWhiteSpace(pi.VoltageSource))
          yield return $"\tНапряжение ПИ: {pi.VoltageSource}";
        else
          yield return $"\tНапряжение ПИ не задано!";

        //  Тип тока
        if (pi.VoltageType != null)
        {
          if (pi.VoltageType == VoltageEnum.Type.ACW)
          {
            yield return $"\tТип тока: переменный";
          }
          else
          {
            yield return $"\tТип тока: постоянный";
          }
        }
        else
          yield return $"\tТип тока не задан!";

        // Время
        if (!string.IsNullOrWhiteSpace(pi.TimeSource))
          yield return $"\tВремя выполнения: {pi.TimeSource}";
        else
          yield return $"\tВремя выполнения не задано!";

        if (pi.Comment.Count > 0)
        {
          yield return $"\tКомментарии:";
          foreach (var line in pi.Comment)
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
        if (pi.Scheme == null || pi.Scheme.IsEmpty())
        {
          yield return "\t\tТочки не заданы!";
          yield break;
        }

        for (int i = 0; i < pi.Scheme.GroupModels.Count; i++)
        {
          var points = pi.Scheme.GetPointsDisconnected(pi.Scheme.GroupModels[i]);
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
      
      yield return string.Empty;
    }
  }
}
