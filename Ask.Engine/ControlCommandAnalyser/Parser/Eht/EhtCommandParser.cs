using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Core.Shared.ParserContext;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.Parser.Helpers;
using Ask.Engine.ControlCommandAnalyser.Parser.Pipeline;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;


namespace Ask.Engine.ControlCommandAnalyser.Parser.Eht
{
  internal class EhtCommandParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic)
        => mnemonic.Mnemonic.MatchesEnum(MeasurementTypeCommand.EHT);

    public BaseCommandModel Parse(
        string commandNumber,
        string mnemonic,
        int numberLine,
        List<string> lines)
    {
      LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}");

      var model = new EhtCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine
      };

      var rmCommandModel = CheckPoints.CheckRm(model, numberLine, commandNumber, mnemonic);

      if (!SourceLinesManager.Check(model, lines, numberLine))
        return model;

      var remainder = PreprocessSourceLines.GetClearCommandBody(model, lines);

      remainder = RemoveCommandPrefix(remainder);

      var ctx = ParameterContext.Create(commandNumber, mnemonic, numberLine);

      remainder = EhtParameterPipeline.Execute(model, remainder, ctx);

      model.Scheme = SchemeManager.GetScheme(model, rmCommandModel, numberLine, ref remainder);

      UnparsedParametersManager.HandleUnparsedParameters(model, numberLine, remainder);

      AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

      LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }

    private static string RemoveCommandPrefix(string remainder)
    {
      var match = Regex.Match(remainder, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s*(.*)$");
      return match.Success ? match.Groups[1].Value.Trim() : remainder;
    }
  }

}

