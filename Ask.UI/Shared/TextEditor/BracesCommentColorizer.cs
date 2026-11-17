using Ask.Core.Services.Config.Base;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System.Windows.Media;

namespace Ask.UI.Shared.TextEditor
{
  /// <summary>
  /// Подсвечивает комментарии:
  /// - { ... } (вложенные)
  /// - /* ... */ (вложенные)
  /// - // до конца строки
  /// </summary>
  public class BracesCommentColorizer : DocumentColorizingTransformer
  {
    private readonly SolidColorBrush _commentBrush = new(Color.FromRgb(87, 166, 74)); // зелёный
    private string _cachedText;
    private List<(int start, int end)> _commentRanges = new();

    protected override void ColorizeLine(DocumentLine line)
    {
      var doc = CurrentContext.Document;

      if (_cachedText != doc.Text)
      {
        _cachedText = doc.Text;
        _commentRanges = ParseCommentsWithPriority(_cachedText);
      }

      int lineStart = line.Offset;
      int lineEnd = lineStart + line.Length;

      foreach (var (start, end) in _commentRanges)
      {
        if (lineStart < end && lineEnd > start)
        {
          int from = Math.Max(lineStart, start);
          int to = Math.Min(lineEnd, end);
          if (UserInterfaceConfig.GetSyntaxHighlighting() == true)
          {
            ChangeLinePart(from, to, e =>
                e.TextRunProperties.SetForegroundBrush(_commentBrush));
          }
        }
      }
    }

    /// <summary>
    /// Парсер фигурных, slash-комментариев и однострочных //.
    /// </summary>
    private static List<(int start, int end)> ParseCommentsWithPriority(string text) =>
      Ask.Core.Services.Translator.ProgramCommentRanges.Parse(text);
  }
}
