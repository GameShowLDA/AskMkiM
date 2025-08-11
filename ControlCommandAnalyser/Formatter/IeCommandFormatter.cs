using ControlCommandAnalyser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Formatter
{
  internal class IeCommandFormatter : ICommandFormatter
  {
    public bool CanFormat(BaseCommandModel model) => model is IeCommandModel;

    public IEnumerable<string> Format(BaseCommandModel model)
    {
      if (model is not IeCommandModel ks)
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

      // Нижний порог электрической емкости
      if (!string.IsNullOrWhiteSpace(ks.LowerLimitCapacity))
      {
        yield return $"\tНижний порог электрической емкости: {ks.LowerLimitCapacity}";
      }
      else
      {
        yield return $"\tНижний порог электрической емкости не задан.";
      }


      // Верхний порог электрической емкости
      if (!string.IsNullOrWhiteSpace(ks.HigherLimitCapacity))
      {
        yield return $"\tВерхний порог электрической емкости: {ks.HigherLimitCapacity}";
      }
      else
      {
        yield return $"\tВерхний порог электрической емкости не задан.";
      }

      if (ks.Points.Count > 0)
      {
        // Точки
        yield return $"\tЗаданные точки:";

        foreach (var point in ks.Points)
          yield return $"\t\t{point}";
      }
      else
      {
        yield return $"\tТочки не заданы!";
      }

      yield return string.Empty;
    }
  }
}
