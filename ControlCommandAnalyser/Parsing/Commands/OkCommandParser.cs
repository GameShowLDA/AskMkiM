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
  public class OkCommandParser : ICommandParser
  {
    public string Mnemonic => "ОК";

    public async Task ParseAsync(CommandBlock block)
    {
      var line = block.Lines.FirstOrDefault()?.Trim();
      if (string.IsNullOrWhiteSpace(line))
      {
        LogWarning($"🔸 Пустая строка в команде ОК (начиная с {block.StartLine + 1})");
      }

      var match = Regex.Match(line, @"^\s*(\d{2,3})\s+ОК", RegexOptions.IgnoreCase);
      if (!match.Success)
      {
        LogError($"❌ Не удалось извлечь номер команды ОК (строка {block.StartLine + 1})");
      }

      string commandNumber = match.Groups[1].Value;

      LogInformation($"🔹 Найдена команда № {commandNumber} ОК — Объект Контроля на строке {block.StartLine + 1}");
    }
  }
}
