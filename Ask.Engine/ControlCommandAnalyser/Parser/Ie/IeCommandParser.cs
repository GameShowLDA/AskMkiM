using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Ie;
using Ask.Engine.ControlCommandAnalyser.Parser.Common;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Ie
{
  /// <summary>
  /// Парсер команды ИЕ (измерение электрической ёмкости).
  /// Выполняет разбор параметров, проверку их порядка,
  /// построение схемы и обработку нераспознанных данных.
  /// </summary>
  [AllowedKeys(Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Д)]
  internal class IeCommandParser : CommandParserBase<IeCommandModel>
  {
    /// <summary>
    /// Определяет, может ли парсер обработать указанную мнемонику.
    /// </summary>
    /// <param name="mnemonic">Идентификатор мнемоники.</param>
    /// <returns>
    /// <c>true</c>, если мнемоника соответствует команде ИЕ; иначе <c>false</c>.
    /// </returns>
    public override bool CanParse(MnemonicIdentifier mnemonic)
      => mnemonic.Mnemonic.MatchesEnum(MeasurementTypeCommand.IE);

    /// <summary>
    /// Создаёт модель команды ИЕ.
    /// </summary>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="numberLine">Номер строки начала команды.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <returns>Экземпляр модели команды ИЕ.</returns>
    protected override IeCommandModel CreateModel(string commandNumber, int numberLine, List<string> lines) => new()
    {
      CommandNumber = commandNumber,
      SourceLines = lines is null ? new List<string>() : new List<string>(lines),
      StartLineNumber = numberLine,
    };

    /// <summary>
    /// Выполняет разбор параметров через конвейер параметров ИЕ.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="remainder">Оставшаяся часть строки команды.</param>
    /// <param name="ctx">Контекст парсинга параметров.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <returns>Строка без обработанных параметров.</returns>
    protected override string ParseParameters(IeCommandModel model, string remainder, ParameterContext ctx, List<string> lines)
      => IeParameterPipeline.Execute(model, remainder, ctx);

    /// <summary>
    /// Выполняет проверку корректности порядка параметров.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="mnemonic">Мнемоника команды.</param>
    /// <param name="numberLine">Номер строки.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <param name="remainder">Оставшаяся часть строки.</param>
    /// <returns>
    /// <c>true</c>, если порядок параметров корректен; иначе <c>false</c>.
    /// </returns>
    protected override bool AfterParametersParsed(
      IeCommandModel model,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines,
      ref string remainder)
    {
      if (InvalidParametersOrderManager.HasInvalidParameterOrder(
            remainder,
            model.AlgorithmKey,
            model.LowerLimitCapacity?.ToString(),
            out string err))
      {
        model.Errors.Add(GeneralErrors.InvalidParameterOrder(mnemonic, numberLine, $"{commandNumber} {mnemonic}", err));
        LogWarning($"Ошибка порядка параметров (строка {numberLine}): {err}");
        return false;
      }

      return true;
    }

    /// <summary>
    /// Выполняет разбор структуры схемы команды.
    /// </summary>
    protected override void ParseStructure(
      IeCommandModel model,
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
    protected override void HandleUnparsed(IeCommandModel model, int numberLine, string remainder)
      => UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);
  }
}
