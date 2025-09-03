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
      // Ключи команды СИ
      if (si.AlgorithmKey.Count > 0)
      {
        yield return $"\tКлючи команды СИ: {string.Join(", ", si.AlgorithmKey)}";
      }
      else
      {
        yield return $"\tКлючи команды СИ не указаны.";
      }

      // Напряжение
      if (!string.IsNullOrWhiteSpace(si.Voltage))
      {
        yield return $"\tНапряжение СИ: {si.Voltage}";
      }
      else
      {
        yield return $"\tНапряжение СИ не задано!";
      }

      // Время
      if (!string.IsNullOrWhiteSpace(si.Time))
      {
        yield return $"\tВремя выполнения СИ: {si.Time}";
      }
      else
      {
        yield return $"\tВремя выполнения СИ не задано!";
      }

      // Сопротивление
      if (!string.IsNullOrWhiteSpace(si.Resistance))
      {
        yield return $"\tСопротивление СИ: {si.Resistance}";
      }
      else
      {
        yield return $"\tСопротивление СИ не задано!";
      }

      // Ключи команды ПИ
      if (pi.AlgorithmKey.Count > 0)
      {
        yield return $"\tКлючи команды ПИ: {string.Join(", ", pi.AlgorithmKey)}";
      }
      else
      {
        yield return $"\tКлючи команды ПИ не указаны.";
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

      yield return "\tЗаданные точки:";

      for (int ci = 0; ci < pi.Scheme.GroupModels.Count; ci++)
      {
        var chain = pi.Scheme.GroupModels[ci];
        if (chain?.ChainModels == null || chain.ChainModels.Count == 0) continue;

        for (int i = 0; i < chain.ChainModels.Count; i++)
        {
          var part = chain.ChainModels[i];
          if (part?.PointModels == null || part.PointModels.Count == 0) continue;

          // первая часть цепи — отметим как начало (*), последующие части считаем "сообщёнными" (#)
          var status = (i == 0) ? "*" : "#";

          foreach (var point in part.PointModels)
          {
            if (part.PointModels.IndexOf(point) == 0 && part.PointModels.Count > 1)
            {
              yield return $"\t\t{status} {point},";
            }
            else if (part.PointModels.IndexOf(point) == 0 && part.PointModels.Count == 1)
            {
              yield return $"\t\t{status} {point}";
            }
            else if (part.PointModels.IndexOf(point) != part.PointModels.Count - 1)
            {
              yield return $"\t\t  {point},";
            }
            else
            {
              yield return $"\t\t  {point}";
            }
          }
        }
      }

      yield return string.Empty;
    }
  }
}
