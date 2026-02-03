using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;


namespace Ask.Engine.ControlCommandAnalyser.Parser.Ck
{
  public class CkCommandParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic) => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.CK);

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {

      LogInformation($"Начало парсинга команды: {commandNumber} {mnemonic}, строк: {lines?.Count ?? 0}");

      var model = new CkCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      var rmCommandModel = CommandsModel.GetRMModel();

      if (rmCommandModel == null)
      {
        LogError($"Команда РМ не найдена");
        model.Errors.Add(PrErrors.EmptyPoints(model.StartLineNumber, $"{model.CommandNumber}   {model.Mnemonic}"));
      }

      if (lines == null || lines.Count == 0)
      {
        LogWarning($"Пустое тело команды: {commandNumber} {mnemonic} (строка {numberLine})");
        model.Errors.Add(PrErrors.EmptyCommandBody(model.StartLineNumber, $"{model.CommandNumber}   {model.Mnemonic}"));
        return model;
      }

      var errors = IndentationCheker.CheckIndentationErrors(lines, commandNumber, mnemonic);
      if (errors.Count > 0)
      {
        foreach (var error in errors)
        {
          LogError(error);
          model.Errors.Add(GeneralErrors.IndentationError(mnemonic, numberLine, $"{commandNumber} {mnemonic}"));
          return model;
        }
      }

      List<string> processedLines = CommentsParser.ParseComments(lines, model);

      model.SourceLines = model.SourceLines
          .Where(l => !string.IsNullOrWhiteSpace(l))
          .ToList();

      var body = string.Concat(processedLines.Count > 0 && processedLines.FindAll(l => string.IsNullOrEmpty(l) || string.IsNullOrWhiteSpace(l)).Count == 0 ?
        processedLines : model.SourceLines)
        .Replace("\r", "")
        .Replace("\n", "")
        .Replace("\t", "");

      LogDebug($"Нормализованное тело команды (в одну строку): \"{body}\"");
      var remainder = body;

      var match = Regex.Match(remainder, @"^\s*\d+\s+[А-ЯA-Z]{2,}\s*(.*)$");
      if (match.Success)
        remainder = match.Groups[1].Value.Trim();

      remainder = KeyParser.ParseKeys(numberLine, model, remainder);

      string bodyNoWs = string.Concat(processedLines.Select(l => Regex.Replace(l ?? string.Empty, @"\s+", "")));

      // Ищем первую и последнюю '*'
      int firstStar = bodyNoWs.IndexOf('*');
      int lastStar = bodyNoWs.LastIndexOf('*');

      if (firstStar >= 0 && lastStar > firstStar)
      {
        // Выделяем блок точек (включительно) — PointParser сам Trim('*')
        string pointsBlob = bodyNoWs.Substring(firstStar, lastStar - firstStar + 1);
        LogDebug($"Парсинг точек из общего блока: '{pointsBlob}'");

        var busList = PointParser.ParseBusList(pointsBlob, rmCommandModel, numberLine, $"{commandNumber} {model.Mnemonic}");

        if (busList.Count > 0)
        {
          model.BusList = busList;
        }
        else
        {
          model.BusList = Enum.GetNames(typeof(SwitchingBus)).ToList();
        }

        int idxStarInFirstLine = remainder.IndexOf('*');
        int idxStarInSecondLine = remainder.LastIndexOf('*');
        if (idxStarInFirstLine >= 0 && idxStarInSecondLine > idxStarInFirstLine)
        {
          remainder =
              remainder[..idxStarInFirstLine].Trim()
              + remainder[(idxStarInSecondLine + 1)..].Trim();
        }
        else
        {
          remainder = remainder.Trim();
        }
      }
      else
      {
        model.BusList = Enum.GetNames(typeof(SwitchingBus)).ToList();
      }

      if (!string.IsNullOrEmpty(remainder))
      {
        model.UnparsedParameters = "! Не распознанные параметры: ";
        model.UnparsedParameters += remainder;
        model.Errors.Add(GeneralErrors.UnrecognizedParameters(remainder, numberLine, $"{commandNumber} {mnemonic}"));
      }

      AllowedKeysAttribute.ValidateKeysAndAttachErrors(model);

      LogInformation($"Завершён парсинг команды: {commandNumber} {mnemonic}");

      return model;
    }
  }
}
