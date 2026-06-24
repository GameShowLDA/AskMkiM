using Ask.Core.Services.Config.AppSettings;
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
      ControlAddressTranslationEngine? translationEngine = null;

      foreach (var sourcePart in sourceParts)
      {
        var partText = sourcePart.Trim();
        if (partText.Length == 0)
          continue;

        var part = CreatePart(partText, hasPartSeparators, model);
        translationEngine ??= new ControlAddressTranslationEngine(CreateTranslationOptions());
        var translation = translationEngine.Translate(part.SourceText);
        foreach (var diagnostic in translation.Diagnostics)
          AddDiagnostic(model, diagnostic);

        var pairs = translation.Entries
          .Select(entry => new RmPairModel
          {
            OkPoint = entry.ObjectAddress.Value,
            Synonym = entry.Synonym?.Value,
            AskInput = entry.MachineAddress.ToString(),
            LegacyAskInput = entry.SourceMachineAddress.ToString()
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

    private static RmTranslationOptions CreateTranslationOptions()
    {
      var modules = Ask.DataBase.Engine.Static.Devices.RelaySwitchModules
        .GetAllAsync()
        .GetAwaiter()
        .GetResult()
        .Select(module => new LegacyRelaySwitchModuleInfo(module.Number, module.PointCount, module.NumberChassis))
        .ToArray();

      if (modules.Length == 0)
        return RmTranslationOptions.Default;

      ILegacyAddressMapper addressMapper = ExecutionConfig.GetIsLegacyCompatibilityModeEnabled()
        ? new RelaySwitchModuleLegacyAddressMapper(modules)
        : new RelaySwitchModuleAddressValidator(modules);

      return new RmTranslationOptions(SynonymBindingMode.ObjectThenSynonym, addressMapper);
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

      if (ContainsPointKey(model, pair.OkPoint))
      {
        return;
      }
      else
      {
        model.PointsMap[pair.OkPoint] = pair.AskInput;
      }

      if (!string.IsNullOrWhiteSpace(pair.Synonym))
      {
        if (ContainsPointKey(model, pair.Synonym))
        {
          return;
        }
        else
        {
          model.SynonymMap[pair.Synonym] = pair.AskInput;
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

  }
}
