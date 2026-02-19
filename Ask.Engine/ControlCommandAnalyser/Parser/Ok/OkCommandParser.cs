using Ask.Core.Services.Errors.Translation;
using Ask.Core.Services.Extensions;
using Ask.Core.Services.Translator;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums.Commands;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text.RegularExpressions;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Ok
{
  /// <summary>
  /// Парсер команды ОК (объект контроля).
  /// Извлекает код и наименование объекта, а также параметры,
  /// описывающие объект контроля.
  /// </summary>
  public class OkCommandParser : ICommandParser
  {
    /// <summary>
    /// Определяет, может ли парсер обработать указанную мнемонику.
    /// </summary>
    /// <param name="mnemonic">Идентификатор мнемоники.</param>
    /// <returns>
    /// <c>true</c>, если мнемоника соответствует команде ОК; иначе <c>false</c>.
    /// </returns>
    public bool CanParse(MnemonicIdentifier mnemonic)
    => mnemonic.Mnemonic.MatchesEnum(OrganizationalComands.OK);

    /// <summary>
    /// Выполняет разбор команды ОК, формируя модель объекта контроля
    /// и извлекая его параметры.
    /// </summary>
    /// <param name="commandNumber">Номер команды.</param>
    /// <param name="mnemonic">Мнемоника команды.</param>
    /// <param name="numberLine">Номер строки начала команды.</param>
    /// <param name="lines">Исходные строки команды.</param>
    /// <returns>Заполненная модель команды ОК.</returns>
    public BaseCommandModel Parse(string commandNumber, string mnemonic, int numberLine, List<string> lines)
    {
      var model = new OkCommandModel
      {
        CommandNumber = commandNumber,
        SourceLines = new List<string>(lines),
        StartLineNumber = numberLine,
      };

      List<string> processedLines = CommentsParser.ParseComments(lines, model);
      model.SourceLines = model.SourceLines
        .Where(l => !string.IsNullOrWhiteSpace(l))
        .ToList();

      if (lines == null || lines.Count == 0)
      {
        model.Errors.Add(OkErrors.EmptyCommandBody(numberLine, $"{commandNumber} {mnemonic}"));
        return model;
      }

      var firstLine = lines[0].Trim();

      var match = Regex.Match(firstLine, @"^\s*(\d+)\s+ОК(\s+.*)?$", RegexOptions.IgnoreCase);
      if (!match.Success)
      {
        model.Errors.Add(OkErrors.CannotParseFirstLine(numberLine, $"{commandNumber} {mnemonic}", firstLine));
        return model;
      }

      var mainPart = match.Groups[2].Success ? match.Groups[2].Value.Trim() : string.Empty;

      string objectCode = string.Empty;
      string objectName = null;

      if (string.IsNullOrWhiteSpace(mainPart))
      {
        model.Errors.Add(OkErrors.MissingObjectCode(numberLine, $"{commandNumber} {mnemonic}"));
        model.ObjectCode = string.Empty;
        model.ObjectName = null;
      }
      else
      {
        if (mainPart.Contains("*"))
        {
          var parts = mainPart.Split(new[] { '*' }, 2, StringSplitOptions.None);
          objectCode = parts[0].Trim();
          objectName = parts.Length > 1 ? parts[1].Trim() : null;
        }
        else
        {
          objectCode = mainPart.Trim();
          objectName = null;
        }


        if (string.IsNullOrWhiteSpace(objectCode))
          model.Errors.Add(OkErrors.MissingObjectCode(numberLine, $"{commandNumber} {mnemonic}"));
        else if (objectCode.Length > 39)
          model.Errors.Add(OkErrors.ObjectCodeTooLong(numberLine, $"{commandNumber} {mnemonic}"));

        model.ObjectCode = objectCode;
        model.ObjectName = objectName;
        model.ControlObjectTitle = objectCode;
        model.ControlObjectName = objectName;
      }


      var uniqueKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      var multiKeys = new HashSet<string> { "ОПК", "КД", "ИК", "ЦЕХ", "ПРИМ", "ПРИМЕЧ", "ПРИМЕЧАНИЕ" };

      for (int i = 1; i < lines.Count; i++)
      {
        var line = lines[i].Trim();
        if (string.IsNullOrWhiteSpace(line))
          continue;

        var paramMatch = Regex.Match(line, @"^([A-Za-zА-Яа-яёЁ0-9_]+)\s*=\s*(.*)$");
        if (!paramMatch.Success)
        {
          model.Errors.Add(OkErrors.CannotParseParameter(numberLine + i, $"{commandNumber} {mnemonic}", line));
          continue;
        }
        var valueParams = paramMatch.Value;
        if (valueParams.Contains("ОПК"))
        {
          var keyParam = paramMatch.Groups[1].Value.Trim().ToUpperInvariant();
          var parsedValueParam = paramMatch.Groups[2].Value.Trim();

          string testProgramTableDesignation = string.Empty;
          string lastMeasurment = null;
          if (parsedValueParam.Contains("*"))
          {
            var parts = parsedValueParam.Split(new[] { '*' }, 2, StringSplitOptions.None);
            testProgramTableDesignation = parts[0].Trim();
            lastMeasurment = parts.Length > 1 ? parts[1].Trim() : null;
          }
          else
          {
            testProgramTableDesignation = parsedValueParam.Trim();
            lastMeasurment = null;
          }
          model.TestProgramTableDesignation = testProgramTableDesignation;
          model.LastMeasurment = lastMeasurment;
        }
        if (valueParams.Contains("КД"))
        {
          if (model.ControlProgramDocument == null)
          {
            model.ControlProgramDocument = new List<string?>();
          }
          if (model.LastNotificationNumber == null)
          {
            model.LastNotificationNumber = new List<Tuple<string, string?>?>();
          }
          var keyParam = paramMatch.Groups[1].Value.Trim().ToUpperInvariant();
          var parsedValueParam = paramMatch.Groups[2].Value.Trim();

          string contolSystemType = string.Empty;
          string controlProgramDocument = null;
          if (parsedValueParam.Contains("*"))
          {
            var parts = parsedValueParam.Split(new[] { '*' }, 2, StringSplitOptions.None);
            contolSystemType = parts[0].Trim();
            controlProgramDocument = parts.Length > 1 ? parts[1].Trim() : null;
          }
          else
          {
            contolSystemType = parsedValueParam.Trim();
            controlProgramDocument = null;
          }
          model.ControlProgramDocument.Add(contolSystemType);
          model.LastNotificationNumber.Add(Tuple.Create(contolSystemType, controlProgramDocument));
        }
        if (valueParams.Contains("ИК"))
        {
          var parsedValueParam = paramMatch.Groups[2].Value.Trim();
          model.ContolSystemType = parsedValueParam;
        }
        if (valueParams.Contains("ЦЕХ"))
        {
          var parsedValueParam = paramMatch.Groups[2].Value.Trim();
          model.DepartmentNumber = parsedValueParam;
        }

        var key = paramMatch.Groups[1].Value.Trim().ToUpperInvariant();
        var value = paramMatch.Groups[2].Value.Trim();

        if (key is "ПРИМЕЧАНИЕ" or "ПРИМЕЧ" or "ПРИМ")
        {
          key = "ПРИМ";

          if (key.Length > 39)
          {
            model.Errors.Add(OkErrors.ParameterKeyTooLong(numberLine + i, $"{commandNumber} {mnemonic}", key));
          }

          int maxLen = key == "ПРИМ" ? 63 : 39;
          if (value.Length > maxLen)
          {
            model.Errors.Add(OkErrors.ParameterValueTooLong(numberLine + i, $"{commandNumber} {mnemonic}", key, maxLen));
          }

          if (!multiKeys.Contains(key))
          {
            if (!uniqueKeys.Add(key))
              model.Errors.Add(OkErrors.DuplicateParameterKey(numberLine + i, $"{commandNumber} {mnemonic}", key));
          }
          model.Comments = value.Trim();
        }
      }

      return model;
    }
  }
}
