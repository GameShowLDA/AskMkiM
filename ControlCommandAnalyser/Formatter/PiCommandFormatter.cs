using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Chains;

namespace ControlCommandAnalyser.Formatter
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
      if (!string.IsNullOrWhiteSpace(si.Voltage))
      {
        yield return $"\t\tНапряжение: {si.Voltage}";
      }
      else
      {
        yield return $"\t\tНапряжение не задано!";
      }

      // Время
      if (!string.IsNullOrWhiteSpace(si.Time))
      {
        yield return $"\t\tВремя выполнения: {si.Time}";
      }
      else
      {
        yield return $"\t\tВремя выполнения не задано!";
      }

      // Сопротивление
      if (!string.IsNullOrWhiteSpace(si.Resistance))
      {
        yield return $"\t\tСопротивление: {si.Resistance}";
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
      if (!string.IsNullOrWhiteSpace(pi.Voltage))
        yield return $"\tНапряжение ПИ: {pi.Voltage}";
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
      if (!string.IsNullOrWhiteSpace(pi.Time))
        yield return $"\tВремя выполнения: {pi.Time}";
      else
        yield return $"\tВремя выполнения не задано!";

      if (pi.Scheme == null || pi.Scheme.IsEmpty())
      {
        yield return "\tТочки не заданы!";
        yield break;
      }

      yield return "\tРазобщенные точки:";
      for (int i = 0; i < pi.Scheme.GroupModels.Count; i++)
      {
        var points = pi.Scheme.GetPointsDisconnected(pi.Scheme.GroupModels[i]);
        if (points != null)
        {
          string str = string.Empty;
          str += $"\t\t{i + 1}. *";

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
