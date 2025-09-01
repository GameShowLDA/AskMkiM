using System.Collections.Generic;
using ControlCommandAnalyser.Model;

namespace ControlCommandAnalyser.Parser.Rm
{
  public class RmCommandParser : ICommandParser
  {
    public bool CanParse(string mnemonic) => mnemonic == "РМ";

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines, RmCommandModel rmCommandModel)
    {
      var model = new RmCommandModel
      {
        CommandNumber = commandNumber,
        Mnemonic = mnemonic,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      // Собрать весь текст команды (все строки после номера и мнемоники)
      var sb = new System.Text.StringBuilder();
      for (int i = 0; i < lines.Count; i++)
      {
        var line = lines[i].Trim();
        if (i == 0)
        {
          // Убрать номер и мнемонику
          var match = System.Text.RegularExpressions.Regex.Match(line, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s*(.*)$");
          if (match.Success) line = match.Groups[1].Value.Trim();
        }
        if (!string.IsNullOrWhiteSpace(line))
          sb.AppendLine(line);
      }

      var pairs = RmExpressionParser.ParseAllExpressions(sb.ToString(), ref model);

      foreach (var pair in pairs)
        model.PointsMap[pair.OkPoint] = pair.AskInput;
      rmCommandModel = model;
      return model;
    }
  }
}
