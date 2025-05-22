using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ControlCommandAnalyser.Domain;
using static Utilities.LoggerUtility;


namespace ControlCommandAnalyser.Parsing.Commands
{
  public class RmCommandParser : ICommandParser
  {
    public string Mnemonic => "РМ";

    public async Task ParseAsync(CommandBlock block)
    {
      var line = block.Lines.FirstOrDefault()?.Trim();
      if (string.IsNullOrWhiteSpace(line))
      {
        LogWarning($"🔸 Пустая строка в команде РМ (начиная с {block.StartLine + 1})");
      }

      var match = Regex.Match(line, @"^\s*(\d{2,3})\s+РМ", RegexOptions.IgnoreCase);
      if (!match.Success)
      {
        LogError($"❌ Не удалось извлечь номер команды РМ (строка {block.StartLine + 1})");
      }

      string commandNumber = match.Groups[1].Value;

      LogInformation($"🔹 Найдена команда № {commandNumber} РМ — Рабочее Место на строке {block.StartLine + 1}");
    }
  }
}
