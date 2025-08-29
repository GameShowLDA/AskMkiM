using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ControlCommandAnalyser.Model;

namespace ControlCommandAnalyser.Parser
{
  /// <summary>
  /// Интерфейс парсера для команды.
  /// </summary>
  public interface ICommandParser
  {
    /// <summary>
    /// Проверяет, подходит ли парсер для данной строки (обычно по мнемонике).
    /// </summary>
    bool CanParse(string mnemonic);

    /// <summary>
    /// Парсит входные строки и возвращает модель команды.
    /// </summary>
    BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines, RmCommandModel rmCommandModel);
  }
}
