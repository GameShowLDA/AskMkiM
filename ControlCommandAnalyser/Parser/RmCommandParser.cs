using System.Collections.Generic;
using System.Text.RegularExpressions;
using ControlCommandAnalyser.Model;

namespace ControlCommandAnalyser.Parser
{
  public class RmCommandParser : ICommandParser
  {
    public bool CanParse(string mnemonic) => mnemonic == "РМ";

    public BaseCommandModel Parse(string commandNumber, string mnemonic, List<string> lines)
    {
      var model = new RmCommandModel
      {
        CommandNumber = commandNumber,
        Mnemonic = mnemonic,
        SourceLines = new List<string>(lines)
      };

      for (int i = 0; i < lines.Count; i++)
      {
        var line = lines[i].Trim();
        if (string.IsNullOrWhiteSpace(line)) continue;

        // Если это первая строка с номером и мнемоникой, убираем их
        if (i == 0)
        {
          // Формат: "15 РМ ..." или "15 PM ..."
          var match = Regex.Match(line, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s+(.*)$");
          if (match.Success)
            line = match.Groups[1].Value.Trim();
        }

        // Дальше универсальный разбор:
        var match2 = Regex.Match(line, @"^([^\s/=]+)/(\d+)(?:-(\d+))?=([\d\.]+)(?:[.-](\d+))?$");
        if (!match2.Success)
          continue;

        string leftPrefix = match2.Groups[1].Value;
        int leftFrom = int.Parse(match2.Groups[2].Value);
        int leftTo = match2.Groups[3].Success ? int.Parse(match2.Groups[3].Value) : leftFrom;

        string rightFull = match2.Groups[4].Value;
        int rightFrom;
        string rightPrefix;
        if (rightFull.Contains('.'))
        {
          rightFrom = int.Parse(rightFull.Split('.').Last());
          rightPrefix = string.Join(".", rightFull.Split('.').Reverse().Skip(1).Reverse());
        }
        else
        {
          rightFrom = int.Parse(rightFull);
          rightPrefix = "";
        }

        int rightTo = match2.Groups[5].Success ? int.Parse(match2.Groups[5].Value) : rightFrom;

        int count = leftTo - leftFrom + 1;
        int rightCount = rightTo - rightFrom + 1;
        if (count != rightCount)
          throw new System.Exception("Диапазоны левой и правой части должны совпадать по длине");

        for (int j = 0; j < count; j++)
        {
          string left = $"{leftPrefix}/{leftFrom + j}";
          string right = string.IsNullOrWhiteSpace(rightPrefix)
            ? $"{rightFrom + j}"
            : $"{rightPrefix}.{rightFrom + j}";
          model.PointsMap[left] = right;
        }
      }
      return model;
    }

  }
}
