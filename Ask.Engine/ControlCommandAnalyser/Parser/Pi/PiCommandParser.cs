using Ask.Core.Services.App;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Common;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Pi
{
  /// <summary>
  /// Парсер команды ПИ.
  /// Выполняет разбор параметров ПИ и вложенной команды СИ,
  /// проверяет наличие пробойной установки и формирует схему.
  /// </summary>
  internal class PiCommandParser : CommandParserBase<PiCommandModel>
  {
    /// <summary>
    /// Определяет, может ли парсер обработать указанную мнемонику.
    /// </summary>
    /// <param name="mnemonic">Идентификатор мнемоники.</param>
    /// <returns>
    /// <c>true</c>, если мнемоника соответствует команде ПИ; иначе <c>false</c>.
    /// </returns>
    public override bool CanParse(MnemonicIdentifier mnemonic)
      => mnemonic.Mnemonic.MatchesEnum(MeasurementTypeCommand.PI);

    /// <summary>
    /// Создаёт модель команды ПИ.
    /// </summary>
    protected override PiCommandModel CreateModel(string commandNumber, int numberLine, List<string> lines) => new()
    {
      CommandNumber = commandNumber,
      SourceLines = lines is null ? new List<string>() : new List<string>(lines),
      StartLineNumber = numberLine,
    };

    /// <summary>
    /// Проверяет наличие пробойной установки перед разбором команды.
    /// </summary>
    protected override bool BeforeCheckRm(
      PiCommandModel model,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines)
    {
      var breakdown = BreakdownTesters.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault();
      if (breakdown == null)
      {
        model.Errors.Add(GeneralErrors.FastMeterNotFound(numberLine, $"{commandNumber} {mnemonic}"));
        return false;
      }
      return true;
    }

    /// <summary>
    /// Определяет, нужно ли удалять префикс команды.
    /// </summary>
    protected override bool ShouldRemoveCommandPrefix(PiCommandModel model) => false;

    /// <summary>
    /// Определяет, требуется ли проверка допустимых ключей.
    /// </summary>
    protected override bool ShouldValidateAllowedKeys(PiCommandModel model) => false;

    /// <summary>
    /// Создаёт контекст парсинга с учётом пробойной установки.
    /// </summary>
    protected override ParameterContext CreateContext(
      string commandNumber,
      string mnemonic,
      int numberLine,
      PiCommandModel model)
      => new(commandNumber, mnemonic, numberLine, BreakdownTesters.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault());

    /// <summary>
    /// Выполняет разбор параметров ПИ и вложенной части СИ.
    /// </summary>
    protected override string ParseParameters(PiCommandModel model, string remainder, ParameterContext ctx, List<string> lines)
    {
      var breakdown = ctx.Breakdown;
      if (breakdown == null)
        return remainder;

      remainder = PiSiSplitter.PreNormalize(remainder);

      LogDebug($"Хвост после ПИ: \"{remainder}\"");
      var (siPart, piPart, errs) = PiSiSplitter.SplitSiFromPiStrict(remainder);
      if (errs.Count > 0)
      {
        LogWarning($"Strict WS issues: {string.Join(" | ", errs)}");
      }

      remainder = TextRemoveManager.RemoveCommandPrefix(remainder);

      var modelSi = new SiCommandModel
      {
        SourceLines = new List<string> { siPart }
      };

      var ctxSi = new ParameterContext(ctx.CommandNumber, ctx.Mnemonic, ctx.LineNumber, breakdown);
      siPart = SiParameterPipeline.Execute(modelSi, siPart, ctxSi, breakdown);
      UnparsedParametersManager.HandleUnparsedParameters(modelSi, ctx.LineNumber, siPart);

      model.SiCommand = modelSi;
      if (modelSi.Errors.Count > 0)
      {
        model.Errors.AddRange(modelSi.Errors);
      }

      var remainderPi = TextRemoveManager.RemoveCommandPrefix(piPart);
      var ctxPi = new ParameterContext(ctx.CommandNumber, ctx.Mnemonic, ctx.LineNumber, breakdown);
      return PiParameterPipeline.Execute(model, remainderPi, ctxPi, breakdown);
    }

    /// <summary>
    /// Выполняет разбор структуры схемы команды.
    /// </summary>
    protected override void ParseStructure(
      PiCommandModel model,
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
    protected override void HandleUnparsed(PiCommandModel model, int numberLine, string remainder)
      => UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);

    /// <summary>
    /// Выполняет финальную синхронизацию данных между ПИ и вложенной СИ.
    /// </summary>
    protected override void AfterParse(
      PiCommandModel model,
      string commandNumber,
      string mnemonic,
      int numberLine,
      List<string> lines)
    {
      if (model.SiCommand == null)
        return;

      model.SiCommand.FormattedStartLineNumber = model.FormattedStartLineNumber;
      model.SiCommand.Scheme = model.Scheme;
      model.SiCommand.CommandNumber = model.CommandNumber;
      model.SiCommand.StartLineNumber = model.StartLineNumber;
    }
  }
}
