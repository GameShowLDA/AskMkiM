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

      yield return "\tЗаданные точки:";

      for (int ci = 0; ci < si.Scheme.ChainModels.Count; ci++)
      {
        var chain = si.Scheme.ChainModels[ci];
        if (chain?.ChainModels == null || chain.ChainModels.Count == 0) continue;

        for (int pi = 0; pi < chain.ChainModels.Count; pi++)
        {
          var part = chain.ChainModels[pi];
          if (part?.PointModels == null || part.PointModels.Count == 0) continue;

          // первая часть цепи — отметим как начало (*), последующие части считаем "сообщёнными" (#)
          var status = (pi == 0) ? "*" : "#";

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
