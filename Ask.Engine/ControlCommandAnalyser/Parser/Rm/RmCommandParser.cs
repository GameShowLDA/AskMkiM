using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Config.Base;
using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Diagnostics;
using Ask.Engine.ControlCommandAnalyser.RmTranslation.Translation;
using System.Text;
using System.Text.RegularExpressions;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Rm
{
  public class RmCommandParser : ICommandParser
  {
    public bool CanParse(MnemonicIdentifier mnemonic)
    => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.RM);

    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      lines ??= new List<string>();
      var model = new RmCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      var bodyBuilder = new StringBuilder();
      var processedLines = CommentsParser.ParseComments(lines, model);
      lines.Clear();
      lines.AddRange(processedLines);

      for (int i = 0; i < lines.Count; i++)
      {
        var line = lines[i].Trim();
        if (i == 0)
        {
          var match = Regex.Match(
            line,
            @"^\s*\d+\s+[\p{L}_]{2,}(?=\s|$)\s*(.*)$",
            RegexOptions.IgnoreCase);
          if (match.Success)
            line = match.Groups[1].Value.Trim();
        }

        if (!string.IsNullOrWhiteSpace(line))
          bodyBuilder.AppendLine(line);
      }

      var body = bodyBuilder.ToString();
      model.PointsSourse = body.Trim();
      var pairs = ParseParts(body, model);
      model.Pairs = pairs;

      if (pairs.Count > 0 && ExecutionConfig.GetIsLegacyCompatibilityModeEnabled())
        InitializeCompatibilityPointsMap(model);

      foreach (var pair in pairs)
        AddPair(model, pair);

      return model;
    }

    private static List<RmPairModel> ParseParts(string body, RmCommandModel model)
    {
      var result = new List<RmPairModel>();
      var hasPartSeparators = body.Contains('*');
      var sourceParts = hasPartSeparators
        ? body.Replace("\r", string.Empty).Split('*')
        : new[] { body };

      foreach (var sourcePart in sourceParts)
      {
        var partText = sourcePart.Trim();
        if (partText.Length == 0)
          continue;

        var part = CreatePart(partText, hasPartSeparators, model);
        var translation = new ControlAddressTranslationEngine().Translate(part.SourceText);
        foreach (var diagnostic in translation.Diagnostics)
          AddDiagnostic(model, diagnostic);

        var pairs = translation.Entries
          .Select(entry => new RmPairModel
          {
            OkPoint = entry.ObjectAddress.Value,
            Synonym = entry.Synonym?.Value,
            AskInput = entry.MachineAddress.ToString()
          })
          .ToList();
        foreach (var pair in pairs)
        {
          pair.PartNumber = part.PartNumber;
          part.Pairs.Add(pair);
          result.Add(pair);
        }

        if (part.Pairs.Count > 0 || part.PartNumber.HasValue)
          model.Parts.Add(part);
      }

      if (result.Count == 0 && model.Errors.Count == 0)
      {
        model.Errors.Add(RmErrors.EmptyCommandBody(
          model.StartLineNumber,
          $"{model.CommandNumber} {model.Mnemonic}"));
      }

      return result;
    }

    private static RmPartModel CreatePart(string partText, bool requirePartNumber, RmCommandModel model)
    {
      var part = new RmPartModel();
      var match = Regex.Match(partText, @"^\s*[Чч]\s*=\s*(?<number>\d+)\b\s*(?<tail>.*)$", RegexOptions.Singleline);

      if (match.Success)
      {
        part.PartNumber = int.Parse(match.Groups["number"].Value);
        part.SourceText = match.Groups["tail"].Value.Trim();
      }
      else
      {
        part.SourceText = partText;
        if (requirePartNumber)
        {
          model.Errors.Add(RmErrors.CannotParseExpression(
            $"Ожидался номер части Ч=...: {partText}",
            model.StartLineNumber,
            $"{model.CommandNumber} {model.Mnemonic}"));
        }
      }

      return part;
    }

    private static void AddPair(RmCommandModel model, RmPairModel pair)
    {
      if (PointModel.ParsePointString(pair.AskInput) == null)
      {
        model.Errors.Add(RmErrors.CannotParseExpression(
          pair.AskInput,
          model.StartLineNumber,
          $"{model.CommandNumber} {model.Mnemonic}"));
        return;
      }

      var askPoint = ExecutionConfig.GetIsLegacyCompatibilityModeEnabled()
        ? LegacyCompatibilityMapper.GetRealAddressByCompatibilityPoint(pair.AskInput)
        : pair.AskInput;

      if (ContainsPointKey(model, pair.OkPoint))
      {
        return;
      }
      else
      {
        model.PointsMap[pair.OkPoint] = askPoint;
      }

      if (!string.IsNullOrWhiteSpace(pair.Synonym))
      {
        if (ContainsPointKey(model, pair.Synonym))
        {
          return;
        }
        else
        {
          model.SynonymMap[pair.Synonym] = askPoint;
        }
      }
    }

    private static bool ContainsPointKey(RmCommandModel model, string pointKey)
    {
      var normalizedKey = RmCommandModel.NormalizePointKey(pointKey);
      return model.PointsMap.Keys
        .Concat(model.SynonymMap.Keys)
        .Any(key => string.Equals(RmCommandModel.NormalizePointKey(key), normalizedKey, StringComparison.OrdinalIgnoreCase));
    }

    private static void AddDiagnostic(RmCommandModel model, RmDiagnostic diagnostic)
    {
      if (diagnostic.Severity != DiagnosticSeverity.Error)
        return;

      model.Errors.Add(RmErrors.CannotParseExpression(
        $"{diagnostic.Code}: {diagnostic.Message}",
        model.StartLineNumber,
        $"{model.CommandNumber} {model.Mnemonic}"));
    }

    private void InitializeCompatibilityPointsMap(RmCommandModel rmCommandModel)
    {
      Dictionary<PointModel, PointModel> CompatibilityPointsMap = new();
      var mkrs = Ask.DataBase.Engine.Static.Devices.RelaySwitchModules.GetAllAsync().GetAwaiter().GetResult().OrderBy(x => x.Number).ToList();

      int numberModule = 1;
      int pointNumber = 1;

      foreach (var item in mkrs)
      {
        for (int i = 1; i <= item.PointCount; i++)
        {
          var askPoint = new PointModel
          {
            DeviceNumber = item.NumberChassis,
            ModuleNumber = item.Number,
            PointNumber = i
          };

          var okPoint = new PointModel
          {
            DeviceNumber = item.NumberChassis,
            ModuleNumber = numberModule,
            PointNumber = pointNumber
          };

          CompatibilityPointsMap[askPoint] = okPoint;

          pointNumber++;
          if (pointNumber > 100)
          {
            pointNumber = 1;
            numberModule++;
          }
        }
      }

      LegacyCompatibilityMapper.SetCompatibilityPointsMap(CompatibilityPointsMap);
    }
  }
}
