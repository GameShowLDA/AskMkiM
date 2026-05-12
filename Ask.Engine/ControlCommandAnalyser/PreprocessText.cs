using System.Text;

namespace Ask.Engine.ControlCommandAnalyser
{
  /// <summary>
  /// Выполняет предварительную обработку текста,
  /// удаляя комментарии и выделяя их в отдельную коллекцию.
  /// </summary>
  internal static class PreprocessText
  {
    /// <summary>
    /// Выполняет разбор текста:
    /// удаляет комментарии из исходного кода
    /// и возвращает найденные комментарии отдельно.
    /// </summary>
    /// <param name="text">
    /// Исходный текст для обработки.
    /// </param>
    /// <returns>
    /// Кортеж, содержащий:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// Словарь строк очищенного кода без комментариев.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Список найденных комментариев с индексами строк.
    /// </description>
    /// </item>
    /// </list>
    /// </returns>
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
    /// Разбивает текст на строки
    /// с нормализацией переносов.
    /// </summary>
    private static List<string> SplitLines(string text) =>
      text.Replace("\r\n", "\n").Split('\n').ToList();

    /// <summary>
    /// Обрабатывает одну строку текста:
    /// выделяет комментарии и сохраняет очищенный код.
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
    /// Обрабатывает содержимое внутри блочного комментария.
    /// </summary>
    private static int HandleInsideComment(string line, int index, CommentContext context, List<(int, string)> comments)
    {
      context.CurrentComment.Append(line[index]);

      if (Match(line, index, "/*"))
      {
        context.Stack.Push("slash");
        context.CurrentComment.Append('*');
        return index + 2;
      }

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
    /// Проверяет начало блочного комментария
    /// (<c>/*</c> или <c>{ }</c>).
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
    /// Проверяет начало однострочного комментария <c>//</c>.
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
    /// Сохраняет код, расположенный перед началом комментария.
    /// </summary>
    private static void SaveCodeBeforeComment(int lineIndex, StringBuilder cleanBuilder, Dictionary<int, string> cleanLines, ref bool commentLine)
    {
      var cleanLine = cleanBuilder.ToString().TrimEnd();
      if (!string.IsNullOrWhiteSpace(cleanLine))
      {
        cleanLines[lineIndex] = cleanLine;
        commentLine = true;
      }
    }

    /// <summary>
    /// Завершает обработку комментария
    /// и добавляет его в результирующую коллекцию.
    /// </summary>
    private static void CloseComment(CommentContext context, List<(int, string)> comments)
    {
      comments.Add((context.StartLine,
                    context.CurrentComment.ToString().TrimEnd()));
      context.CurrentComment.Clear();
      context.StartLine = -1;
    }

    /// <summary>
    /// Завершает обработку строки,
    /// сохраняя очищенный код при необходимости.
    /// </summary>
    private static void FinalizeCleanLine(int lineIndex, StringBuilder cleanBuilder, CommentContext context, Dictionary<int, string> cleanLines, bool commentLine)
    {
      if (!context.InComment)
      {
        var cleanLine = cleanBuilder.ToString().TrimEnd();
        if (!string.IsNullOrWhiteSpace(cleanLine))
          cleanLines[lineIndex] = cleanLine;
        else if (commentLine)
          cleanLines.Remove(lineIndex);
      }
    }

    /// <summary>
    /// Добавляет незакрытый комментарий,
    /// если файл завершился до его закрытия.
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
    /// Проверяет совпадение подстроки
    /// с указанным значением по заданному индексу.
    /// </summary>
    private static bool Match(string line, int index, string value) =>
      index < line.Length - (value.Length - 1) &&
      line.Substring(index, value.Length) == value;

    /// <summary>
    /// Хранит текущее состояние обработки комментариев.
    /// </summary>
    private sealed class CommentContext
    {
      /// <summary>
      /// Стек вложенности комментариев.
      /// </summary>
      public Stack<string> Stack { get; } = new();

      /// <summary>
      /// Накопитель текста текущего комментария.
      /// </summary>
      public StringBuilder CurrentComment { get; } = new();

      /// <summary>
      /// Индекс строки,
      /// в которой начался текущий комментарий.
      /// </summary>
      public int StartLine { get; set; } = -1;

      /// <summary>
      /// Возвращает <c>true</c>,
      /// если в данный момент выполняется обработка комментария.
      /// </summary>
      public bool InComment => Stack.Count > 0;
    }
  }
}
