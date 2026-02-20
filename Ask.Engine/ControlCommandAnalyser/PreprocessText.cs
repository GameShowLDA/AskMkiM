using System.Text;

namespace Ask.Engine.ControlCommandAnalyser
{
  internal static class PreprocessText
  {
    /// <summary>
    /// Главная точка входа.
    /// Возвращает:
    ///  - словарь строк без комментариев
    ///  - список найденных комментариев с индексами строк
    /// </summary>
    public static (Dictionary<int, string> CleanLines, List<(int LineIndex, string Text)> Comments)
      PreprocessTextAndExtractComments(string text)
    {
      var lines = SplitLines(text);
      var cleanLines = new Dictionary<int, string>();
      var comments = new List<(int, string)>();

      var context = new CommentContext();

      for (int i = 0; i < lines.Count; i++)
      {
        ProcessLine(lines[i], i, context, cleanLines, comments);
      }

      FinalizeUnclosedComment(lines.Count, context, comments);

      return (cleanLines, comments);
    }

    /// <summary>
    /// Разбивает текст на строки с нормализацией переносов.
    /// </summary>
    private static List<string> SplitLines(string text) =>
      text.Replace("\r\n", "\n").Split('\n').ToList();

    /// <summary>
    /// Обрабатывает одну строку:
    /// определяет комментарии и формирует очищенную строку.
    /// </summary>
    private static void ProcessLine(string line, int lineIndex, CommentContext context, Dictionary<int, string> cleanLines, List<(int, string)> comments)
    {
      int index = 0;
      var cleanBuilder = new StringBuilder();
      bool commentLine = false;

      while (index < line.Length)
      {
        if (context.InComment)
        {
          index = HandleInsideComment(line, index, context, comments);
          continue;
        }

        if (TryStartBlockComment(line, ref index, lineIndex,
                                 cleanBuilder, context, cleanLines, ref commentLine))
          continue;

        if (TryStartLineComment(line, index, lineIndex,
                        cleanBuilder, cleanLines, comments, ref commentLine))
          break;

        cleanBuilder.Append(line[index]);
        index++;
      }

      FinalizeCleanLine(lineIndex, cleanBuilder, context, cleanLines, commentLine);

      if (context.InComment)
        context.CurrentComment.Append('\n');
    }

    /// <summary>
    /// Обработка символов, когда парсер находится внутри комментария.
    /// </summary>
    private static int HandleInsideComment(string line, int index, CommentContext context, List<(int, string)> comments)
    {
      context.CurrentComment.Append(line[index]);

      // вложенный /*
      if (Match(line, index, "/*"))
      {
        context.Stack.Push("slash");
        context.CurrentComment.Append('*');
        return index + 2;
      }

      // закрытие */
      if (Match(line, index, "*/") && context.Stack.Peek() == "slash")
      {
        context.Stack.Pop();
        context.CurrentComment.Append('/');
        index += 2;

        if (!context.InComment)
          CloseComment(context, comments);

        return index;
      }

      if (line[index] == '{')
        context.Stack.Push("brace");
      else if (line[index] == '}' && context.Stack.Peek() == "brace")
      {
        context.Stack.Pop();
        if (!context.InComment)
          CloseComment(context, comments);
      }

      return index + 1;
    }

    /// <summary>
    /// Пытается начать блочный комментарий (/* или { }).
    /// </summary>
    private static bool TryStartBlockComment(string line, ref int index, int lineIndex, StringBuilder cleanBuilder, CommentContext context,
      Dictionary<int, string> cleanLines, ref bool commentLine)
    {
      if (Match(line, index, "/*"))
      {
        SaveCodeBeforeComment(lineIndex, cleanBuilder, cleanLines, ref commentLine);

        context.Stack.Push("slash");
        context.CurrentComment.Clear();
        context.CurrentComment.Append("/*");
        context.StartLine = lineIndex;

        index += 2;
        return true;
      }

      if (line[index] == '{')
      {
        SaveCodeBeforeComment(lineIndex, cleanBuilder, cleanLines, ref commentLine);

        context.Stack.Push("brace");
        context.CurrentComment.Clear();
        context.CurrentComment.Append('{');
        context.StartLine = lineIndex;

        index++;
        return true;
      }

      return false;
    }

    /// <summary>
    /// Пытается обработать однострочный комментарий //.
    /// </summary>
    private static bool TryStartLineComment(string line, int index, int lineIndex, StringBuilder cleanBuilder, Dictionary<int, string> cleanLines,
      List<(int, string)> comments, ref bool commentLine)
    {
      if (!Match(line, index, "//"))
        return false;

      if (cleanBuilder.Length > 0)
      {
        cleanLines[lineIndex] = cleanBuilder.ToString().TrimEnd();
        commentLine = true;
      }

      comments.Add((lineIndex, line.Substring(index).TrimEnd()));

      return true;
    }


    /// <summary>
    /// Сохраняет код, который был перед началом комментария.
    /// </summary>
    private static void SaveCodeBeforeComment(int lineIndex, StringBuilder cleanBuilder, Dictionary<int, string> cleanLines, ref bool commentLine)
    {
      if (cleanBuilder.Length > 0)
      {
        cleanLines[lineIndex] = cleanBuilder.ToString().TrimEnd();
        commentLine = true;
      }
    }

    /// <summary>
    /// Завершает комментарий и добавляет его в список.
    /// </summary>
    private static void CloseComment(CommentContext context, List<(int, string)> comments)
    {
      comments.Add((context.StartLine,
                    context.CurrentComment.ToString().TrimEnd()));
      context.CurrentComment.Clear();
      context.StartLine = -1;
    }

    /// <summary>
    /// Сохраняет очищенную строку, если она содержит код.
    /// </summary>
    private static void FinalizeCleanLine(int lineIndex, StringBuilder cleanBuilder, CommentContext context, Dictionary<int, string> cleanLines, bool commentLine)
    {
      if (!context.InComment)
      {
        var cleanLine = cleanBuilder.ToString().TrimEnd();
        if (!string.IsNullOrWhiteSpace(cleanLine) && !commentLine)
          cleanLines[lineIndex] = cleanLine;
      }
    }

    /// <summary>
    /// Если комментарий не был закрыт до конца файла — добавляет его.
    /// </summary>
    private static void FinalizeUnclosedComment(int totalLines, CommentContext context, List<(int, string)> comments)
    {
      if (context.InComment && context.CurrentComment.Length > 0)
      {
        comments.Add((context.StartLine != -1 ? context.StartLine : totalLines - 1,
                      context.CurrentComment.ToString().TrimEnd()));
      }
    }

    /// <summary>
    /// Проверяет совпадение подстроки начиная с позиции.
    /// </summary>
    private static bool Match(string line, int index, string value) =>
      index < line.Length - (value.Length - 1) &&
      line.Substring(index, value.Length) == value;

    /// <summary>
    /// Контекст текущего состояния парсинга комментариев.
    /// </summary>
    private sealed class CommentContext
    {
      public Stack<string> Stack { get; } = new();
      public StringBuilder CurrentComment { get; } = new();
      public int StartLine { get; set; } = -1;
      public bool InComment => Stack.Count > 0;
    }
  }
}
