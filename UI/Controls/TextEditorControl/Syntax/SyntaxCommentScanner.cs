using System;
using ICSharpCode.AvalonEdit.Document;

namespace UI.Controls.TextEditorControl.Syntax
{
  public static class SyntaxCommentScanner
  {
    public static IReadOnlyList<TextSpan> Scan(TextDocument document)
    {
      var result = new List<TextSpan>();

      string text = document.Text;

      bool isBlockComment = false;
      int blockCommentStart = -1;

      int i = 0;

      while (i < text.Length)
      {
        if (isBlockComment)
        {
          if (text[i] == '}')
          {
            int length = i - blockCommentStart + 1;
            result.Add(new TextSpan(blockCommentStart, length));

            isBlockComment = false;
            blockCommentStart = -1;
          }

          i++;
          continue;
        }

        // Однострочный комментарий //
        if (i + 1 < text.Length && text[i] == '/' && text[i + 1] == '/')
        {
          var line = document.GetLineByOffset(i);

          int start = i;
          int end = line.EndOffset;

          result.Add(new TextSpan(start, end - start));

          i = end;
          continue;
        }

        // Многострочный комментарий { ... }
        if (text[i] == '{')
        {
          isBlockComment = true;
          blockCommentStart = i;

          i++;
          continue;
        }

        i++;
      }

      // Если открыли {, но не закрыли до конца файла,
      // считаем всё до конца файла комментарием.
      if (isBlockComment && blockCommentStart >= 0)
      {
        result.Add(new TextSpan(
          blockCommentStart,
          text.Length - blockCommentStart));
      }

      return result;
    }

    public static string RemoveCommentsFromLine(
      string lineText,
      int lineOffset,
      IReadOnlyList<TextSpan> commentSpans)
    {
      if (string.IsNullOrEmpty(lineText))
        return lineText;

      char[] chars = lineText.ToCharArray();

      int lineStart = lineOffset;
      int lineEnd = lineOffset + lineText.Length;

      foreach (var span in commentSpans)
      {
        int overlapStart = Math.Max(lineStart, span.StartOffset);
        int overlapEnd = Math.Min(lineEnd, span.EndOffset);

        if (overlapStart >= overlapEnd)
          continue;

        int startIndex = overlapStart - lineOffset;
        int endIndex = overlapEnd - lineOffset;

        for (int i = startIndex; i < endIndex; i++)
        {
          chars[i] = ' ';
        }
      }

      return new string(chars);
    }
  }
}
