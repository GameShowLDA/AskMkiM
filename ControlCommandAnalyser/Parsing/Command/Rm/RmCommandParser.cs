using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ControlCommandAnalyser.Domain;
using ControlCommandAnalyser.Parsing.Interface;
using ControlCommandAnalyser.Parsing.Model;
using Utilities.TextEditor;

namespace ControlCommandAnalyser.Parsing.Command.Rm
{
  /// <summary>
  /// Парсер для команды РМ.
  /// </summary>
  public class RmCommandParser : ICommandParser
  {
    public string Mnemonic => "РМ";

    /// <summary>
    /// Парсит блок команды РМ.
    /// </summary>
    public CommandBlock? Parse(CommandBlock originalBlock, out List<HighlightRange> highlights)
    {
      highlights = new List<HighlightRange>();
      // Парсим только строку с мнемоникой РМ
      var lines = originalBlock.Lines.Skip(1).ToList();

      var model = new RmCommandModel
      {
        CommandNumber = originalBlock.CommandNumber,
        Mnemonic = Mnemonic,
        PointsMap = new Dictionary<string, string>()
      };

      foreach (var line in lines)
      {
        var match = Regex.Match(line.Trim(), @"^([^\s/=]+)/(\d+)-(\d+)=([\d\.]+)-(\d+)$");
        if (match.Success)
        {
          var prefix = match.Groups[1].Value;
          var from = int.Parse(match.Groups[2].Value);
          var to = int.Parse(match.Groups[3].Value);
          var rightPrefix = match.Groups[4].Value;
          var rightFrom = int.Parse(match.Groups[5].Value) - (to - from);
          // Формируем пары точек
          for (int i = from, j = rightFrom; i <= to; i++, j++)
            model.PointsMap.Add($"{prefix}/{i}", $"{rightPrefix}.{j}");
          continue;
        }

        match = Regex.Match(line.Trim(), @"^([^\s/=]+)/(\d+)-(\d+)=([\d\.]+)\.(\d+)-(\d+)$");
        if (match.Success)
        {
          var prefix = match.Groups[1].Value;
          var from = int.Parse(match.Groups[2].Value);
          var to = int.Parse(match.Groups[3].Value);
          var rightPrefix = match.Groups[4].Value;
          var rightFrom = int.Parse(match.Groups[5].Value);
          var rightTo = int.Parse(match.Groups[6].Value);
          int count = to - from + 1;
          for (int i = 0; i < count; i++)
            model.PointsMap.Add($"{prefix}/{from + i}", $"{rightPrefix}.{rightFrom + i}");
          continue;
        }

        match = Regex.Match(line.Trim(), @"^([^\s/=]+)/(\d+)=([\d\.]+)$");
        if (match.Success)
        {
          var prefix = match.Groups[1].Value;
          var idx = int.Parse(match.Groups[2].Value);
          var target = match.Groups[3].Value;
          model.PointsMap.Add($"{prefix}/{idx}", target);
          continue;
        }
      }

      // Если удалось — возвращаем новый блок (или модель сохраняем как нужно)
      return originalBlock; // Можно заменить на новый CommandBlock, если требуется обновление
    }
  }
}
