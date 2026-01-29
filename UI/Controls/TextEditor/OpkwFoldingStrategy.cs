using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using System.Text.RegularExpressions;

namespace UI.Controls.TextEditor
{
  public class OpkwFoldingStrategy
  {
    // Только номер команды + допустимая мнемоника
    private static readonly Regex CommandHeaderRegex = new(
      @"^[ \t]*(\d+)[ \t]+(СИ|ОК|ВШ|ЭТ|СП|РМ|ЦУ|ПР|ПИ|КЦ|УП|КС)\b",
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

        int endOffset;
        if (i + 1 < matches.Count)
        {
          int nextDigitsIndex = matches[i + 1].Groups[1].Index;
          var nextLine = document.GetLineByOffset(nextDigitsIndex);
          endOffset = nextLine.Offset - 1;
        }
        else
        {
          endOffset = document.TextLength;
        }

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