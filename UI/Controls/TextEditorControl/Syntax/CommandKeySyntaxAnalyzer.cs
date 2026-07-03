using System;
using System.Text.RegularExpressions;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser;
using ICSharpCode.AvalonEdit.Document;

namespace UI.Controls.TextEditorControl.Syntax
{
  /// <summary>
  /// Проверяет алгоритмические ключи команды в исходном тексте редактора.
  /// </summary>
  public static class CommandKeySyntaxAnalyzer
  {
    private static readonly Regex KeyRegex = new(
      BuildKeyPattern(),
      RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static IReadOnlyList<TextSyntaxDiagnostic> Analyze(
      TextDocument document,
      BaseCommandModel model,
      int endLineNumber,
      IReadOnlyList<TextSpan> commentSpans)
    {
      var allowedKeys = GetAllowedKeys(model);
      if (allowedKeys.Count == 0 || document.LineCount == 0)
        return Array.Empty<TextSyntaxDiagnostic>();

      if (string.Equals(model.Mnemonic, "ЦУ", StringComparison.OrdinalIgnoreCase))
        return AnalyzeCuKeys(document, model, allowedKeys, commentSpans);

      var diagnostics = new List<TextSyntaxDiagnostic>();
      var allowedSet = allowedKeys.ToHashSet();
      var startLineNumber = Math.Clamp(model.StartLineNumber, 1, document.LineCount);
      endLineNumber = Math.Clamp(endLineNumber, startLineNumber, document.LineCount);
      var firstPointOffset = FindFirstPointOffset(document, startLineNumber, endLineNumber, commentSpans);

      for (int lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
      {
        var line = document.GetLineByNumber(lineNumber);
        string lineText = SyntaxCommentScanner.RemoveCommentsFromLine(
          document.GetText(line),
          line.Offset,
          commentSpans);

        foreach (Match match in KeyRegex.Matches(lineText))
        {
          if (!Enum.TryParse<AlgorithmKey>(
                match.Groups["key"].Value,
                ignoreCase: true,
                out var key))
          {
            continue;
          }

          if (!allowedSet.Contains(key))
          {
            if (!HasExistingKeyIssue(model, match.Groups["key"].Value))
            {
              diagnostics.Add(CreateDiagnostic(
                "KEY001",
                $"Ключ {match.Groups["key"].Value} недопустим для команды {model.Mnemonic}. Допустимые ключи: {FormatKeys(allowedKeys)}.",
                line,
                match));
            }

            continue;
          }

          if (line.Offset + match.Index > firstPointOffset)
          {
            diagnostics.Add(CreateDiagnostic(
              "KEY002",
              $"Ключ {match.Groups["key"].Value} должен быть указан до блока точек команды {model.Mnemonic}.",
              line,
              match));
          }
        }
      }

      return diagnostics;
    }

    private static IReadOnlyList<TextSyntaxDiagnostic> AnalyzeCuKeys(
      TextDocument document,
      BaseCommandModel model,
      IReadOnlyList<AlgorithmKey> allowedKeys,
      IReadOnlyList<TextSpan> commentSpans)
    {
      var lineNumber = Math.Clamp(model.StartLineNumber, 1, document.LineCount);
      var line = document.GetLineByNumber(lineNumber);
      string lineText = SyntaxCommentScanner.RemoveCommentsFromLine(
        document.GetText(line),
        line.Offset,
        commentSpans);
      var headerMatch = Regex.Match(
        lineText,
        @"^\s*\d+\s+ЦУ\s+(?<key>[^\s,;]+)",
        RegexOptions.IgnoreCase);

      if (!headerMatch.Success ||
          !Enum.TryParse<AlgorithmKey>(
            headerMatch.Groups["key"].Value,
            ignoreCase: true,
            out var key) ||
          allowedKeys.Contains(key))
      {
        return Array.Empty<TextSyntaxDiagnostic>();
      }

      return new[]
      {
        CreateDiagnostic(
          "KEY001",
          $"Ключ {headerMatch.Groups["key"].Value} недопустим для команды {model.Mnemonic}. Допустимые ключи: {FormatKeys(allowedKeys)}.",
          line,
          headerMatch.Groups["key"])
      };
    }

    private static TextSyntaxDiagnostic CreateDiagnostic(
      string code,
      string message,
      DocumentLine line,
      Capture capture)
    {
      return new TextSyntaxDiagnostic
      {
        Code = code,
        Severity = TextSyntaxSeverity.Error,
        Message = message,
        StartOffset = line.Offset + capture.Index,
        Length = capture.Length,
        LineNumber = line.LineNumber,
        ColumnNumber = capture.Index + 1
      };
    }

    private static int FindFirstPointOffset(
      TextDocument document,
      int startLineNumber,
      int endLineNumber,
      IReadOnlyList<TextSpan> commentSpans)
    {
      for (int lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
      {
        var line = document.GetLineByNumber(lineNumber);
        string lineText = SyntaxCommentScanner.RemoveCommentsFromLine(
          document.GetText(line),
          line.Offset,
          commentSpans);
        int pointIndex = lineText.IndexOf('*');

        if (pointIndex >= 0)
          return line.Offset + pointIndex;
      }

      return int.MaxValue;
    }

    private static bool HasExistingKeyIssue(BaseCommandModel model, string key)
    {
      return model.Errors.Cast<IDisplayIssue>()
        .Concat(model.Warnings)
        .Any(issue =>
          issue.Description?.Contains(key, StringComparison.OrdinalIgnoreCase) == true &&
          issue.CodeString?.Contains("Key", StringComparison.OrdinalIgnoreCase) == true);
    }

    private static IReadOnlyList<AlgorithmKey> GetAllowedKeys(BaseCommandModel model)
    {
      return model.AllowedAlgorithmKeys.Count > 0
        ? model.AllowedAlgorithmKeys
        : KeysHelper.GetAllowedKeysForModel(model);
    }

    private static string FormatKeys(IEnumerable<AlgorithmKey> keys)
      => string.Join(", ", keys.OrderBy(key => key.ToString()));

    private static string BuildKeyPattern()
    {
      var keys = Enum.GetNames<AlgorithmKey>()
        .OrderByDescending(key => key.Length)
        .Select(Regex.Escape);

      return $@"(?<![\p{{L}}\p{{N}}/])(?<key>{string.Join("|", keys)})(?![\p{{L}}\p{{N}}/])";
    }
  }
}
