using System;
using ICSharpCode.AvalonEdit.Document;

namespace UI.Controls.TextEditorControl.Syntax
{
  public sealed class EditorSyntaxAnalyzer
  {
    private readonly CommandHeaderSyntaxAnalyzer _headerAnalyzer;

    private readonly CommandTranslationSyntaxAnalyzer? _translationSyntaxAnalyzer;

    /// <summary>
    /// Создаёт анализатор текста редактора.
    /// </summary>
    /// <param name="knownMnemonics">Мнемоники, допустимые в заголовках команд.</param>
    /// <param name="translationSyntaxAnalyzer">Необязательный анализатор на основе общего движка трансляции.</param>
    public EditorSyntaxAnalyzer(
      IEnumerable<string> knownMnemonics,
      CommandTranslationSyntaxAnalyzer? translationSyntaxAnalyzer = null)
    {
      _headerAnalyzer = new CommandHeaderSyntaxAnalyzer(knownMnemonics);
      _translationSyntaxAnalyzer = translationSyntaxAnalyzer;
    }

    /// <summary>
    /// Анализирует документ и возвращает все найденные синтаксические диагностики.
    /// </summary>
    /// <param name="document">Документ AvalonEdit.</param>
    /// <returns>Список диагностик с абсолютными смещениями в документе.</returns>
    public IReadOnlyList<TextSyntaxDiagnostic> Analyze(TextDocument document)
    {
      var diagnostics = new List<TextSyntaxDiagnostic>();

      var commentSpans = SyntaxCommentScanner.Scan(document);

      bool hasPreviousCommand = false;

      var usedCommandNumbers =
        new Dictionary<string, CommandHeaderInfo>(StringComparer.OrdinalIgnoreCase);

      foreach (var line in document.Lines)
      {
        string originalLineText = document.GetText(line);

        string lineTextWithoutComments =
          SyntaxCommentScanner.RemoveCommentsFromLine(
            originalLineText,
            line.Offset,
            commentSpans);

        if (string.IsNullOrWhiteSpace(lineTextWithoutComments))
          continue;

        var lineDiagnostics = _headerAnalyzer.AnalyzeLine(
          line,
          lineTextWithoutComments,
          hasPreviousCommand,
          out var header);

        diagnostics.AddRange(lineDiagnostics);

        if (header == null)
          continue;

        hasPreviousCommand = true;

        if (usedCommandNumbers.TryGetValue(header.CommandNumber, out var previousHeader))
        {
          diagnostics.Add(new TextSyntaxDiagnostic
          {
            Code = "CMD007",
            Severity = TextSyntaxSeverity.Error,
            Message = $"Номер команды {header.CommandNumber} уже используется на строке {previousHeader.LineNumber}.",
            StartOffset = header.NumberStartOffset,
            Length = header.CommandNumber.Length,
            LineNumber = header.LineNumber,
            ColumnNumber = header.NumberStartOffset - header.LineOffset + 1
          });
        }
        else
        {
          usedCommandNumbers.Add(header.CommandNumber, header);
        }
      }

      if (_translationSyntaxAnalyzer != null && !HasBlockingHeaderDiagnostics(diagnostics))
      {
        AddTranslationDiagnostics(document, diagnostics);
      }

      return diagnostics;
    }

    private void AddTranslationDiagnostics(
      TextDocument document,
      List<TextSyntaxDiagnostic> diagnostics)
    {
      var translationDiagnostics = _translationSyntaxAnalyzer!.Analyze(document);

      foreach (var diagnostic in translationDiagnostics)
      {
        if (ShouldSuppressDiagnostic(diagnostic, diagnostics))
        {
          continue;
        }

        diagnostics.Add(diagnostic);
      }
    }

    private static bool ShouldSuppressDiagnostic(
      TextSyntaxDiagnostic candidate,
      IReadOnlyList<TextSyntaxDiagnostic> existingDiagnostics)
    {
      return existingDiagnostics.Any(existing =>
        IsSameDiagnostic(existing, candidate)
        || IsUnknownCommandDuplicate(existing, candidate));
    }

    private static bool HasBlockingHeaderDiagnostics(
      IReadOnlyList<TextSyntaxDiagnostic> diagnostics)
    {
      return diagnostics.Any(diagnostic =>
        diagnostic.Severity == TextSyntaxSeverity.Error &&
        diagnostic.Code.StartsWith("CMD", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSameDiagnostic(
      TextSyntaxDiagnostic left,
      TextSyntaxDiagnostic right)
    {
      return string.Equals(left.Code, right.Code, StringComparison.OrdinalIgnoreCase)
             && left.StartOffset == right.StartOffset
             && left.Length == right.Length
             && string.Equals(left.Message, right.Message, StringComparison.Ordinal);
    }

    private static bool IsUnknownCommandDuplicate(
      TextSyntaxDiagnostic existing,
      TextSyntaxDiagnostic candidate)
    {
      return string.Equals(existing.Code, "CMD006", StringComparison.OrdinalIgnoreCase)
             && string.Equals(candidate.Code, "Gen_UnknownCommand", StringComparison.OrdinalIgnoreCase)
             && RangesOverlap(existing, candidate);
    }

    private static bool RangesOverlap(
      TextSyntaxDiagnostic left,
      TextSyntaxDiagnostic right)
    {
      int leftEnd = left.StartOffset + left.Length;
      int rightEnd = right.StartOffset + right.Length;

      return left.StartOffset < rightEnd && right.StartOffset < leftEnd;
    }
  }
}
