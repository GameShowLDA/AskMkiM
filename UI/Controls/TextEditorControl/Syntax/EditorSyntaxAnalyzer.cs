using System;
using ICSharpCode.AvalonEdit.Document;

namespace UI.Controls.TextEditorControl.Syntax
{
  public sealed class EditorSyntaxAnalyzer
  {
    private readonly CommandHeaderSyntaxAnalyzer _headerAnalyzer;

    public EditorSyntaxAnalyzer(IEnumerable<string> knownMnemonics)
    {
      _headerAnalyzer = new CommandHeaderSyntaxAnalyzer(knownMnemonics);
    }

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

      return diagnostics;
    }
  }
}
