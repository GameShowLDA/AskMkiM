using System;
using ICSharpCode.AvalonEdit.Document;

namespace UI.Controls.TextEditorControl.Syntax
{
  /// <summary>
  /// Находит все виды комментариев, поддерживаемые командным транслятором.
  /// </summary>
  public static class SyntaxCommentScanner
  {
    public static IReadOnlyList<TextSpan> Scan(TextDocument document)
    {
      var result = new List<TextSpan>();
      var stack = new Stack<CommentKind>();
      string text = document.Text;
      int commentStart = -1;
      int index = 0;

      while (index < text.Length)
      {
        if (stack.Count == 0)
        {
          if (StartsWith(text, index, "//"))
          {
            var line = document.GetLineByOffset(index);
            result.Add(new TextSpan(index, line.EndOffset - index));
            index = line.EndOffset;
            continue;
          }

          if (StartsWith(text, index, "/*"))
          {
            commentStart = index;
            stack.Push(CommentKind.Slash);
            index += 2;
            continue;
          }

          if (text[index] == '{')
          {
            commentStart = index;
            stack.Push(CommentKind.Brace);
            index++;
            continue;
          }

          index++;
          continue;
        }

        if (StartsWith(text, index, "/*"))
        {
          stack.Push(CommentKind.Slash);
          index += 2;
          continue;
        }

        if (text[index] == '{')
        {
          stack.Push(CommentKind.Brace);
          index++;
          continue;
        }

        if (StartsWith(text, index, "*/") && stack.Peek() == CommentKind.Slash)
        {
          stack.Pop();
          index += 2;
          AddCompletedSpan(result, commentStart, index, stack);
          continue;
        }

        if (text[index] == '}' && stack.Peek() == CommentKind.Brace)
        {
          stack.Pop();
          index++;
          AddCompletedSpan(result, commentStart, index, stack);
          continue;
        }

        index++;
      }

      if (stack.Count > 0 && commentStart >= 0)
      {
        result.Add(new TextSpan(commentStart, text.Length - commentStart));
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
      int lineEnd = lineOffset + lineText.Length;

      foreach (var span in commentSpans)
      {
        int overlapStart = Math.Max(lineOffset, span.StartOffset);
        int overlapEnd = Math.Min(lineEnd, span.EndOffset);

        if (overlapStart >= overlapEnd)
          continue;

        for (int index = overlapStart - lineOffset;
             index < overlapEnd - lineOffset;
             index++)
        {
          chars[index] = ' ';
        }
      }

      return new string(chars);
    }

    private static void AddCompletedSpan(
      ICollection<TextSpan> result,
      int start,
      int end,
      IReadOnlyCollection<CommentKind> stack)
    {
      if (stack.Count == 0 && start >= 0 && end > start)
      {
        result.Add(new TextSpan(start, end - start));
      }
    }

    private static bool StartsWith(string text, int index, string value)
    {
      return index + value.Length <= text.Length &&
             text.AsSpan(index, value.Length).SequenceEqual(value);
    }

    private enum CommentKind
    {
      Brace,
      Slash
    }
  }
}
