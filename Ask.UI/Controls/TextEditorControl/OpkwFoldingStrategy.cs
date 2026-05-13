using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using System.Text.RegularExpressions;

namespace Ask.UI.Controls.TextEditorControl
{
  public class OpkwFoldingStrategy
  {
    // Заголовок команды: номер + мнемоника.
    // Используем общее правило (как в парсере), чтобы не пропускать
    // валидные команды при расширении набора мнемоник.
    private static readonly Regex CommandHeaderRegex = new(
      @"^[ \t]*(\d+)[ \t]+([А-ЯЁA-Z]{2,})\b",
      RegexOptions.Compiled | RegexOptions.Multiline);

    public void UpdateFoldings(FoldingManager manager, TextDocument document)
    {
      var newFoldings = new List<NewFolding>();
      var matches = CommandHeaderRegex.Matches(document.Text);

      for (int i = 0; i < matches.Count; i++)
      {
        int digitsIndex = matches[i].Groups[1].Index;
        var startLine = document.GetLineByOffset(digitsIndex);
        int startOffset = startLine.Offset;

        DocumentLine? endLine;
        if (i + 1 < matches.Count)
        {
          int nextDigitsIndex = matches[i + 1].Groups[1].Index;
          var nextLine = document.GetLineByOffset(nextDigitsIndex);
          endLine = nextLine.PreviousLine;
        }
        else
        {
          endLine = document.LineCount > 0
            ? document.GetLineByNumber(document.LineCount)
            : null;
        }

        // Сворачиваем только тело команды (хотя бы одна строка после заголовка).
        if (endLine == null || endLine.LineNumber <= startLine.LineNumber)
          continue;

        int endOffset = endLine.EndOffset;
        if (endOffset > startOffset)
        {
          string header = $"{matches[i].Groups[1].Value} {matches[i].Groups[2].Value}";
          newFoldings.Add(new NewFolding(startOffset, endOffset) { Name = header });
        }
      }

      manager.UpdateFoldings(newFoldings, -1);
    }
  }
}
