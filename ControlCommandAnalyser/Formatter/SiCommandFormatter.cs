using System.Collections.Generic;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Chains;

namespace ControlCommandAnalyser.Formatter
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
      if (!string.IsNullOrWhiteSpace(si.Voltage))
      {
        yield return $"\tНапряжение: {si.Voltage}";
      }
      else
      {
        yield return $"\tНапряжение не задано!";
      }

      // Время
      if (!string.IsNullOrWhiteSpace(si.Time))
      {
        yield return $"\tВремя выполнения: {si.Time}";
      }
      else
      {
        yield return $"\tВремя выполнения не задано!";
      }

      // Сопротивление
      if (!string.IsNullOrWhiteSpace(si.Resistance))
      {
        yield return $"\tСопротивление: {si.Resistance}";
      }
      else
      {
        yield return $"\tСопротивление не задано!";
      }

      if (si.Scheme == null || si.Scheme.IsEmpty())
      {
        yield return "\tТочки не заданы!";
        yield break;
      }

      yield return "\tСообщенные точки:";
      foreach (var item in si.Scheme.GroupModels)
      {
        var points = si.Scheme.GetPointsConnected(item);
        if (points != null)
        {
          for (int i = 0; i < points.Count; i++)
          {
            string str = string.Empty;
            str += $"\t\t{i + 1}. ";
            foreach (var point in points[i])
            {
              str += $"{point.Mnemonic}({point}), ";
            }

            yield return str.Remove(str.Length - 2);
          }
        }
      }

      yield return "\tРазобщенные точки:";
      for (int i = 0; i < si.Scheme.GroupModels.Count; i++)
      {
        var points = si.Scheme.GetPointsDisconnected(si.Scheme.GroupModels[i]);
        if (points != null)
        {
          string str = string.Empty;
          str += $"\t\t{i+1}. #";

          foreach (var point in points)
          {
            str += $"{point.Mnemonic}({point})#";
          }
          yield return str.Remove(str.Length - 1);
        }
      }


      yield return string.Empty;
    }
  }
}
