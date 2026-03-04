using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Executor;

namespace Ask.Engine.ControlCommandAnalyser.Parser
{
  /// <summary>
  /// Интерфейс парсера для команды.
  /// </summary>
  public interface ICommandParser
  {
    /// <summary>
    /// Проверяет, подходит ли парсер для данной строки (обычно по мнемонике).
    /// </summary>
    bool CanParse(MnemonicIdentifier mnemonic);

    /// <summary>
    /// Парсит входные строки и возвращает модель команды.
    /// </summary>
    BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines);
  }
}
