using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline;


namespace Ask.Engine.ControlCommandAnalyser.Parser.Eht
{
  /// <summary>
  /// Парсер команды ЭТ.
  /// Реализует разбор параметров, структуры схемы и обработку
  /// нераспознанных параметров на основе базового парсера.
  /// </summary>
  internal class EhtCommandParser : CommandParserBase<EhtCommandModel>
  {
    /// <summary>
    /// Определяет, может ли парсер обработать указанную мнемонику.
    /// </summary>
    /// <param name="mnemonic">Идентификатор мнемоники.</param>
    /// <returns>
    /// <c>true</c>, если мнемоника соответствует команде ЭТ; иначе <c>false</c>.
    /// </returns>
    public override bool CanParse(MnemonicIdentifier mnemonic)
      => mnemonic.Mnemonic.MatchesEnum(MeasurementTypeCommand.EHT);

    /// <summary>
    /// Создаёт модель команды ЭТ.
    /// </summary>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="numberLine">Номер строки начала команды.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <returns>Экземпляр модели команды ЭТ.</returns>
    protected override EhtCommandModel CreateModel(string commandNumber, int numberLine, List<string> lines) => new()
    {
      CommandNumber = commandNumber,
      SourceLines = lines is null ? new List<string>() : new List<string>(lines),
      StartLineNumber = numberLine
    };

    /// <summary>
    /// Выполняет разбор параметров команды через конвейер параметров.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <returns>Строка без обработанных параметров.</returns>
    protected override string ParseParameters(EhtCommandModel model, string remainder, ParameterContext ctx, List<string> lines)
      => EhtParameterPipeline.Execute(model, remainder, ctx);

    /// <summary>
    /// Выполняет разбор структуры схемы команды.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="rmCommandModel">Модель команды РМ.</param>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="mnemonic">Мнемоника команды.</param>
    /// <param name="numberLine">Номер строки начала команды.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    protected override void ParseStructure(
      EhtCommandModel model,
      RmCommandModel rmCommandModel,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines,
      ref string remainder)
      => model.Scheme = SchemeManager.GetScheme(model, rmCommandModel, numberLine, ref remainder);

    /// <summary>
    /// Обрабатывает нераспознанные параметры команды.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="numberLine">Номер строки команды.</param>
    /// <param name="remainder">Оставшаяся неразобранная часть строки.</param>
    protected override void HandleUnparsed(EhtCommandModel model, int numberLine, string remainder)
      => UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);
  }
}


