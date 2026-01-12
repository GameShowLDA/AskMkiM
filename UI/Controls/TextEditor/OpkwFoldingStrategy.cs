using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using System.Text.RegularExpressions;

namespace UI.Controls.TextEditor
{
  public class OpkwFoldingStrategy
  {
    // Только номер команды + допустимая мнемоника
    private static readonly Regex CommandHeaderRegex = new(
      @"^\s*(\d+)\s+(СИ|ОК|СП|РМ|ЦУ|ПР|ПИ|КЦ|УП|КС)\b",
      RegexOptions.Compiled | RegexOptions.Multiline);

    public void UpdateFoldings(FoldingManager manager, TextDocument document)
    {
      var newFoldings = new List<NewFolding>();
      var matches = CommandHeaderRegex.Matches(document.Text);

      for (int i = 0; i < matches.Count; i++)
      {
        int startOffset = matches[i].Index;
        int endOffset = (i + 1 < matches.Count)
            ? matches[i + 1].Index - 1
            : document.TextLength;

        string header = $"{matches[i].Groups[1].Value} {matches[i].Groups[2].Value}";
        if (endOffset > startOffset)
          newFoldings.Add(new NewFolding(startOffset, endOffset) { Name = header });
      }

      manager.UpdateFoldings(newFoldings, -1);
    }
  }
}