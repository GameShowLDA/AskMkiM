using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ControlCommandAnalyser.Domain;
using ControlCommandAnalyser.Parsing.Interface;
using static Utilities.LoggerUtility;

namespace ControlCommandAnalyser.Parsing.Commands
{
  public class OkCommandParser : ICommandParser
  {
    public string Mnemonic => "ОК";

    public async Task ParseAsync(CommandBlock block)
    {
      var line = block.Lines.FirstOrDefault();

      if (!CommandHeaderParser.TryParseHeader(line, out string commandNumber, out _))
      {
        LogError($"❌ Не удалось разобрать заголовок команды ОК (строка {block.StartLine + 1})");
        return;
      }

      LogInformation($"🔹 Найдена команда № {commandNumber} ОК — Объект Контроля на строке {block.StartLine + 1}");
    }
  }
}
