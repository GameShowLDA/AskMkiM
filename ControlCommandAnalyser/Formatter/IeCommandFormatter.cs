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

      if(string.IsNullOrWhiteSpace(ie.LowerLimitCapacity)&& string.IsNullOrWhiteSpace(ie.HigherLimitCapacity))
      {
        yield return $"\tЭлектрическая емкость не задана!";
      }
      // Нижний порог электрической емкости
      else if (!string.IsNullOrWhiteSpace(ie.LowerLimitCapacity))
      {
        yield return $"\tНижний порог электрической емкости: {ie.LowerLimitCapacity}";
      }
      // Верхний порог электрической емкости
      else if(!string.IsNullOrWhiteSpace(ie.HigherLimitCapacity))
      {
        yield return $"\tВерхний порог электрической емкости: {ie.HigherLimitCapacity}";
      }

      if (ie.Points.Count > 0)
      {
        yield return $"\tЗаданные точки:";
        foreach (var pointModel in ie.Points)
        {
          foreach (var point in pointModel.Points)
          {
            var status = string.Empty;
            if (pointModel.Status == true)
            {
              status = "#";
            }
            else
            {
              status = "*";
            }
            yield return $"\t\t{status} {point}";
          }
        }
      }
      else
      {
        yield return $"\tТочки не заданы!";
      }

      yield return string.Empty;
    }
  }
}
