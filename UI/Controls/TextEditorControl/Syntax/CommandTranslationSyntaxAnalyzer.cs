using Ask.Core.Services.Errors.Models;
using Ask.Core.Shared.DTO.Executor;
using Ask.Engine.ControlCommandAnalyser;
using ICSharpCode.AvalonEdit.Document;

namespace UI.Controls.TextEditorControl.Syntax
{
  /// <summary>
  /// Выполняет командный анализ текста редактора через общий движок разбора
  /// программ контроля и преобразует найденные ошибки в диапазоны AvalonEdit.
  /// </summary>
  public sealed class CommandTranslationSyntaxAnalyzer
  {
    private const string AnalyzerFailureCode = "CMD900";

    private readonly CommandTranslationManager _translationManager;

    /// <summary>
    /// Создаёт анализатор командных диагностик.
    /// </summary>
    /// <param name="translationManager">Менеджер трансляции, используемый как источник правил разбора.</param>
    public CommandTranslationSyntaxAnalyzer(CommandTranslationManager translationManager)
    {
      _translationManager = translationManager ?? throw new ArgumentNullException(nameof(translationManager));
    }

    /// <summary>
    /// Анализирует документ и возвращает диагностики, построенные по ошибкам
    /// и предупреждениям командных моделей.
    /// </summary>
    /// <param name="document">Документ AvalonEdit.</param>
    /// <returns>Список диагностик с абсолютными смещениями в документе.</returns>
    public IReadOnlyList<TextSyntaxDiagnostic> Analyze(TextDocument document)
    {
      if (document == null || string.IsNullOrWhiteSpace(document.Text))
      {
        return Array.Empty<TextSyntaxDiagnostic>();
      }

      try
      {
        var models = _translationManager.ParseForDiagnostics(document.Text);
        var commentSpans = SyntaxCommentScanner.Scan(document);
        return BuildDiagnostics(document, models, commentSpans);
      }
      catch (Exception ex)
      {
        return new[]
        {
          CreateAnalyzerFailureDiagnostic(document, ex)
        };
      }
    }

    private static IReadOnlyList<TextSyntaxDiagnostic> BuildDiagnostics(
      TextDocument document,
      IEnumerable<BaseCommandModel> models,
      IReadOnlyList<TextSpan> commentSpans)
    {
      var diagnostics = new List<TextSyntaxDiagnostic>();

      foreach (var model in models)
      {
        foreach (var error in model.Errors)
        {
          var diagnostic = CreateDiagnostic(document, model, error, commentSpans);
          if (diagnostic != null)
          {
            diagnostics.Add(diagnostic);
          }
        }

        foreach (var warning in model.Warnings)
        {
          var diagnostic = CreateDiagnostic(document, model, warning, commentSpans);
          if (diagnostic != null)
          {
            diagnostics.Add(diagnostic);
          }
        }
      }

      return diagnostics;
    }

    private static TextSyntaxDiagnostic? CreateDiagnostic(
      TextDocument document,
      BaseCommandModel model,
      IDisplayIssue issue,
      IReadOnlyList<TextSpan> commentSpans)
    {
      if (!TryResolveIssueSpan(document, model, issue, commentSpans, out var span))
      {
        return null;
      }

      return new TextSyntaxDiagnostic
      {
        Code = issue.CodeString ?? (issue.IsWarning ? "WRN_UNKNOWN" : "ERR_UNKNOWN"),
        Message = issue.Description,
        Severity = issue.IsWarning ? TextSyntaxSeverity.Warning : TextSyntaxSeverity.Error,
        StartOffset = span.StartOffset,
        Length = span.Length,
        LineNumber = span.LineNumber,
        ColumnNumber = span.ColumnNumber
      };
    }

    private static TextSyntaxDiagnostic CreateAnalyzerFailureDiagnostic(
      TextDocument document,
      Exception exception)
    {
      var firstLine = document.GetLineByNumber(1);

      return new TextSyntaxDiagnostic
      {
        Code = AnalyzerFailureCode,
        Message = $"Не удалось выполнить синтаксический анализ команд: {exception.Message}",
        Severity = TextSyntaxSeverity.Warning,
        StartOffset = firstLine.Offset,
        Length = Math.Max(1, firstLine.Length),
        LineNumber = 1,
        ColumnNumber = 1
      };
    }

    private static bool TryResolveIssueSpan(
      TextDocument document,
      BaseCommandModel model,
      IDisplayIssue issue,
      IReadOnlyList<TextSpan> commentSpans,
      out CommandIssueSpan span)
    {
      int lineNumber = ResolveLineNumber(document, model, issue);
      var line = document.GetLineByNumber(lineNumber);
      string lineText = document.GetText(line);
      string lineTextWithoutComments = SyntaxCommentScanner.RemoveCommentsFromLine(
        lineText,
        line.Offset,
        commentSpans);

      if (IssueSelectionHintResolver.TryResolve(issue, lineTextWithoutComments, out var hint)
          && IsValidHint(lineTextWithoutComments, hint))
      {
        span = new CommandIssueSpan(
          line.Offset + hint.StartIndex,
          hint.Length,
          lineNumber,
          hint.StartIndex + 1);
        return true;
      }

      if (TryResolveCommandHeaderSpan(lineTextWithoutComments, model, issue, out hint)
          && IsValidHint(lineTextWithoutComments, hint))
      {
        span = new CommandIssueSpan(
          line.Offset + hint.StartIndex,
          hint.Length,
          lineNumber,
          hint.StartIndex + 1);
        return true;
      }

      return TryResolveNonWhiteSpaceLineSpan(line, lineTextWithoutComments, out span);
    }

    private static int ResolveLineNumber(
      TextDocument document,
      BaseCommandModel model,
      IDisplayIssue issue)
    {
      int lineNumber = issue.SourceLineNumber > 0
        ? issue.SourceLineNumber
        : model.StartLineNumber;

      if (lineNumber <= 0)
      {
        lineNumber = 1;
      }

      return Math.Clamp(lineNumber, 1, document.LineCount);
    }

    private static bool TryResolveCommandHeaderSpan(
      string lineText,
      BaseCommandModel model,
      IDisplayIssue issue,
      out IssueSelectionHint hint)
    {
      foreach (var candidate in GetHeaderCandidates(model, issue))
      {
        int index = lineText.IndexOf(candidate, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
          continue;
        }

        hint = new IssueSelectionHint(index, candidate.Length);
        return true;
      }

      hint = default;
      return false;
    }

    private static IEnumerable<string> GetHeaderCandidates(
      BaseCommandModel model,
      IDisplayIssue issue)
    {
      if (!string.IsNullOrWhiteSpace(issue.Command))
      {
        yield return issue.Command;
      }

      if (!string.IsNullOrWhiteSpace(model.CommandNumber)
          && !string.IsNullOrWhiteSpace(model.Mnemonic))
      {
        yield return $"{model.CommandNumber} {model.Mnemonic}";
      }

      if (!string.IsNullOrWhiteSpace(model.Mnemonic))
      {
        yield return model.Mnemonic;
      }

      if (!string.IsNullOrWhiteSpace(model.CommandNumber))
      {
        yield return model.CommandNumber;
      }
    }

    private static bool TryResolveNonWhiteSpaceLineSpan(
      DocumentLine line,
      string lineText,
      out CommandIssueSpan span)
    {
      int start = 0;
      while (start < lineText.Length && char.IsWhiteSpace(lineText[start]))
      {
        start++;
      }

      int end = lineText.Length;
      while (end > start && char.IsWhiteSpace(lineText[end - 1]))
      {
        end--;
      }

      if (end <= start)
      {
        span = default;
        return false;
      }

      span = new CommandIssueSpan(
        line.Offset + start,
        end - start,
        line.LineNumber,
        start + 1);
      return true;
    }

    private static bool IsValidHint(string lineText, IssueSelectionHint hint)
    {
      return hint.StartIndex >= 0
             && hint.Length > 0
             && hint.StartIndex + hint.Length <= lineText.Length;
    }

    private readonly record struct CommandIssueSpan(
      int StartOffset,
      int Length,
      int LineNumber,
      int ColumnNumber);
  }
}
