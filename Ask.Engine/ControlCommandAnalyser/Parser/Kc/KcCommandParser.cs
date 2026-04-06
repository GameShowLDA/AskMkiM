using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.HelperParserParametr;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Kc
{
  /// <summary>
  /// Парсер команды КС (контроль сопротивления).
  /// Выполняет полный цикл разбора: подготовку строк,
  /// обработку параметров, построение схемы и валидацию ключей.
  /// </summary>
  [AllowedKeys(Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Б, Ask.Core.Shared.Metadata.Enums.TranslationEnums.AlgorithmKey.Д)]
  internal class KcCommandParser : ICommandParser
  {
    /// <summary>
    /// Определяет, может ли парсер обработать указанную мнемонику.
    /// </summary>
    /// <param name="mnemonic">Идентификатор мнемоники.</param>
    /// <returns>
    /// <c>true</c>, если мнемоника соответствует команде КС; иначе <c>false</c>.
    /// </returns>
    public bool CanParse(MnemonicIdentifier mnemonic) => mnemonic.Mnemonic.MatchesEnum(MeasurementTypeCommand.KC);

    /// <summary>
    /// Выполняет разбор команды КС, включая обработку параметров,
    /// построение схемы и проверку ошибок.
    /// </summary>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="mnemonic">Мнемоника команды.</param>
    /// <param name="numberLine">Номер строки начала команды.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <returns>Заполненная модель команды КС.</returns>
    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");

      var model = new KsCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = lines == null ? new List<string>() : new List<string>(lines),
        StartLineNumber = numberLine,
      };

      if (lines == null || lines.Count == 0 || lines.All(string.IsNullOrWhiteSpace))
      {
        model.Errors.Add(KsErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
        return model;
      }

      var rmCommandModel = CheckPoints.CheckRm(model, numberLine, commandNumber, mnemonic);
      if (!SourceLinesManager.Check(model, lines, numberLine))
        return model;
      var remainder = PreprocessSourceLines.GetClearCommandBody(model, lines);
      remainder = TextRemoveManager.RemoveCommandPrefix(remainder);

      if (string.IsNullOrWhiteSpace(remainder))
      {
        model.Errors.Add(KsErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
        return model;
      }

      if (HasPointsBeforeResistance(remainder))
      {
        model.Errors.Add(KsErrors.InvalidParameterOrder(numberLine, $"{commandNumber} {mnemonic}"));
      }

      var ctx = ParameterContext.Create(commandNumber, mnemonic, numberLine);
      var meter = FastMeters.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault();
      remainder = KsParameterPipeline.Execute(model, remainder, ctx, meter);
      model.Scheme = SchemeManager.GetScheme(model, rmCommandModel, numberLine, ref remainder);

      UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);
      AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);
      NormalizeKsSpecificErrors(model, numberLine, $"{commandNumber} {mnemonic}");

      LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }

    private static bool HasPointsBeforeResistance(string remainder)
    {
      if (string.IsNullOrWhiteSpace(remainder))
      {
        return false;
      }

      var compact = remainder.Replace(" ", string.Empty);
      int firstStar = compact.IndexOf('*');
      int lastStar = compact.LastIndexOf('*');

      if (firstStar < 0 || lastStar <= firstStar)
      {
        return false;
      }

      var beforePoints = compact[..firstStar];
      var afterPoints = compact[(lastStar + 1)..];

      var beforeRange = CommonParameterParser.ResistanceParser.ParseResistanceRange(beforePoints);
      if (!string.IsNullOrWhiteSpace(beforeRange.Min) || !string.IsNullOrWhiteSpace(beforeRange.Max))
      {
        return false;
      }

      var afterRange = CommonParameterParser.ResistanceParser.ParseResistanceRange(afterPoints);
      return !string.IsNullOrWhiteSpace(afterRange.Min) || !string.IsNullOrWhiteSpace(afterRange.Max);
    }

    private static void NormalizeKsSpecificErrors(KsCommandModel model, int numberLine, string commandId)
    {
      ReplaceError(model, error =>
          error.Code == ErrorCode.Gen_NoPointsBody
          || error.Code == ErrorCode.Gen_EmptyPointsBody,
        KsErrors.EmptyPoints(numberLine, commandId));

      if (model.Errors.Any(error => error.Code == ErrorCode.Ks_CannotParseParameters))
      {
        if (model.Errors.Any(error => error.Code == ErrorCode.Gen_UnrecognizedParameters))
        {
          model.Errors.RemoveAll(error => error.Code == ErrorCode.Ks_EmptyResistance);
        }
        else
        {
          model.Errors.RemoveAll(error => error.Code == ErrorCode.Ks_CannotParseParameters);
        }
      }
    }

    private static void ReplaceError(KsCommandModel model, Predicate<ErrorItem> predicate, ErrorItem replacement)
    {
      if (!model.Errors.Any(error => predicate(error)))
      {
        return;
      }

      model.Errors.RemoveAll(predicate);
      model.Errors.Add(replacement);
    }
  }
}

