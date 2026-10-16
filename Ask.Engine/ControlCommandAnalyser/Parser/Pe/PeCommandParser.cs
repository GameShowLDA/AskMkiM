using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Model.Pr;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Common.Pipeline;
using static Ask.LogLib.LoggerUtility;
using static System.Net.Mime.MediaTypeNames;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Pe
{
  /// <summary>
  /// Парсер команды ПЭ (проверка электронная).
  /// </summary>
  internal class PeCommandParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic) => mnemonic.Mnemonic.MatchesEnum(MeasurementTypeCommand.PE);


    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");

      var model = new PeCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = lines is null ? new List<string>() : new List<string>(lines),
        StartLineNumber = numberLine,
      };

      var rmCommandModel = CheckPoints.CheckRm(model, numberLine, commandNumber, mnemonic);
      if (!SourceLinesManager.Check(model, lines, numberLine))
        return model;
      var remainder = PreprocessSourceLines.GetClearCommandBody(model, lines);
      remainder = TextRemoveManager.RemoveCommandPrefix(remainder);

      var ctx = ParameterContext.Create(commandNumber, mnemonic, numberLine);
      var meter = FastMeters.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault();
      remainder = PeParameterPipeline.Execute(model, remainder, ctx, meter);
      model.Scheme = SchemeManager.GetScheme(model, rmCommandModel, numberLine, ref remainder);
      UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);
      AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);


      var prModel = new PrCommandModel
      {
        CommandNumber = model.CommandNumber,
        StartLineNumber = model.StartLineNumber,
        Scheme = model.Scheme,
      };

      string text = string.Join("", lines);
      string[] parts = text.Split('*', 2);

      string firstPart = parts[0].Trim();
      string secondPart = parts.Length > 1 ? "*" + parts[1].Trim() : "";

      if (model.AlgorithmKey.Contains(AlgorithmKey.ЗС.GetDescription()))
      {
        prModel.SourceLines = new List<string>{ firstPart, " Ом>80", secondPart };
      }
      else
      {
        prModel.SourceLines = new List<string> { firstPart, " Ом<80", secondPart };
      }
      
      var prRemainder = PreprocessSourceLines.GetClearCommandBody(prModel, prModel.SourceLines);
      prRemainder = TextRemoveManager.RemoveCommandPrefix(prRemainder);
      prRemainder = PrParameterPipeline.Execute(prModel, prRemainder, ctx, meter);
      prModel.Scheme = SchemeManager.GetScheme(prModel, rmCommandModel, numberLine, ref prRemainder);
      UnparsedParametersManager.HandleUnparsedParameters(prModel, numberLine, prRemainder);
      AllowedKeysAttribute.ValidateKeysAndAttachErrors(prModel);

      LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");
      prModel.Warnings.Add(GeneralWarnings.PeToPrCommandAddedAutomatically(prModel.StartLineNumber, $"{prModel.CommandNumber} {prModel.Mnemonic}"));

      return prModel;
    }
  }
}
