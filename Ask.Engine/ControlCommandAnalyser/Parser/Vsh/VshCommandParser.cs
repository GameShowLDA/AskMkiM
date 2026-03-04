using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Vsh
{
  /// <summary>
  /// Парсер команды ВШ (описание структуры шин).
  /// <para>
  /// Выполняет:
  /// <list type="bullet">
  /// <item><description>удаление комментариев из исходных строк;</description></item>
  /// <item><description>проверку на пустое тело команды;</description></item>
  /// <item><description>разбор структуры шин через <c>BusStructureParser</c>;</description></item>
  /// <item><description>нормализацию списка исходных строк модели.</description></item>
  /// </list>
  /// </para>
  /// </summary>
  public class VshCommandParser : ICommandParser
  {
    /// <summary>
    /// Проверяет, может ли парсер обработать команду
    /// с указанной мнемоникой.
    /// </summary>
    /// <param name="mnemonic">Идентификатор мнемоники команды.</param>
    /// <returns>
    /// true — если мнемоника соответствует команде ВШ;  
    /// false — если команда не поддерживается данным парсером.
    /// </returns>
    public bool CanParse(MnemonicIdentifier mnemonic)
    => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.VSH);

    /// <summary>
    /// Выполняет разбор команды ВШ и формирует модель.
    /// <para>
    /// Алгоритм:
    /// <list type="number">
    /// <item><description>Создаёт модель команды.</description></item>
    /// <item><description>Удаляет комментарии из строк.</description></item>
    /// <item><description>Проверяет наличие тела команды.</description></item>
    /// <item><description>Парсит структуру шин.</description></item>
    /// <item><description>Удаляет пустые строки из SourceLines.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="mnemonic">Мнемоника команды.</param>
    /// <param name="numberLine">Номер строки начала команды.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <returns>
    /// Заполненная модель <see cref="VshCommandModel"/> с результатами разбора и возможными ошибками.
    /// </returns>
    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      var model = new VshCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      List<string> processedLines = CommentsParser.ParseComments(lines, model);

      if (lines == null || lines.Count == 0)
      {
        LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
        model.Errors.Add(SiErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
      }

      model = BusStructureParser.ParseVshCommand(string.Join(Environment.NewLine, processedLines), model);

      model.SourceLines = model.SourceLines
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .ToList();

      return model;
    }
  }
}
