using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Domain;
using ControlCommandAnalyser.Parsing.Interface;
using static Utilities.LoggerUtility;

namespace ControlCommandAnalyser.Parsing.Commands
{
  public class PrCommandParser : ICommandParser
  {
    public string Mnemonic => "ПР";

    public async Task ParseAsync(CommandBlock block)
    {
      var line = block.Lines.FirstOrDefault();

      if (!CommandHeaderParser.TryParseHeader(line, out string commandNumber, out _))
      {
        LogError($"❌ Не удалось разобрать заголовок команды ПР (строка {block.StartLine + 1})");
        return;
      }

      LogInformation($"🔹 Найдена команда № {commandNumber} ПР — ПРоверка релейная на строке {block.StartLine + 1}");
    }
  }
}
