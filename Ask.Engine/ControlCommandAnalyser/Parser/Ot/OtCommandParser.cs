using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Ot
{
  /// <summary>
  /// Парсер команды ОТ.
  /// Выполняет разбор параметров, извлекает точки по шинам
  /// и обрабатывает нераспознанные параметры.
  /// </summary>
  internal class OtCommandParser : CommandParserBase<OtCommandModel>
  {
    /// <summary>
    /// Определяет, может ли парсер обработать указанную мнемонику.
    /// </summary>
    /// <param name="mnemonic">Идентификатор мнемоники.</param>
    /// <returns>
    /// <c>true</c>, если мнемоника соответствует команде ОТ; иначе <c>false</c>.
    /// </returns>
    public override bool CanParse(MnemonicIdentifier mnemonic)
      => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.OT);

    /// <summary>
    /// Создаёт модель команды ОТ.
    /// </summary>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="numberLine">Номер строки начала команды.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <returns>Экземпляр модели команды ОТ.</returns>
    protected override OtCommandModel CreateModel(string commandNumber, int numberLine, List<string> lines) => new()
    {
      CommandNumber = commandNumber,
      SourceLines = lines is null ? new List<string>() : new List<string>(lines),
      StartLineNumber = numberLine,
    };

    /// <summary>
    /// Выполняет разбор параметров через конвейер параметров ОТ.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <returns>Строка без обработанных параметров.</returns>
    protected override string ParseParameters(OtCommandModel model, string remainder, ParameterContext ctx, List<string> lines)
      => OtParameterPipeline.Execute(model, remainder, ctx);

    /// <summary>
    /// Выполняет разбор структуры точек по шинам.
    /// </summary>
    protected override void ParseStructure(
      OtCommandModel model,
      RmCommandModel rmCommandModel,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines,
      ref string remainder)
      => model.BusPointsDictionary = SchemeManager.GetBusPointsDictionary(
        model,
        rmCommandModel,
        numberLine,
        commandNumber,
        mnemonic,
        ref remainder);

    /// <summary>
    /// Обрабатывает нераспознанные параметры команды.
    /// </summary>
    protected override void HandleUnparsed(OtCommandModel model, int numberLine, string remainder)
      => UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);
  }
}
