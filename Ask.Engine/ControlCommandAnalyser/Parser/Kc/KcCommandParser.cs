using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Ks;
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
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };
      var rmCommandModel = CheckPoints.CheckRm(model, numberLine, commandNumber, mnemonic);
      if (!SourceLinesManager.Check(model, lines, numberLine))
        return model;
      var remainder = PreprocessSourceLines.GetClearCommandBody(model, lines);
      remainder = TextRemoveManager.RemoveCommandPrefix(remainder);

      var ctx = ParameterContext.Create(commandNumber, mnemonic, numberLine);
      var meter = new DataBaseConfiguration.Services.Device.FastMeterServices().GetAll().FirstOrDefault();
      remainder = KsParameterPipeline.Execute(model, remainder, ctx, meter);
      model.Scheme = SchemeManager.GetScheme(model, rmCommandModel, numberLine, ref remainder);

      UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);
      AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

      LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }
  }
}

