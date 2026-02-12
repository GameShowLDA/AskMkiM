using System.Text;

namespace Ask.Engine.ControlCommandAnalyser
{
  internal class PreprocessText
  {
    /// <summary>
    /// Возвращает очищенные строки программы и список всех найденных комментариев.
    /// </summary>
    public static (Dictionary<int, string> CleanLines, List<(int LineIndex, string Text)> Comments)
    PreprocessTextAndExtractComments(string text)
    {
      var lines = text.Replace("\r\n", "\n").Split('\n').ToList();
      var cleanLines = new Dictionary<int, string>();
      var comments = new List<(int LineIndex, string Text)>();

      var stack = new Stack<string>();
      var currentComment = new StringBuilder();
      int commentStartLine = -1;

      for (int i = 0; i < lines.Count; i++)
      {
        string line = lines[i];
        int index = 0;
        var cleanBuilder = new StringBuilder();
        bool commentLine = false;

        while (index < line.Length)
        {
          // === внутри комментария ===
          if (stack.Count > 0)
          {
            currentComment.Append(line[index]);

            // вложенный /* */
            if (index < line.Length - 1 &&
                line[index] == '/' &&
                line[index + 1] == '*')
            {
              stack.Push("slash");
              currentComment.Append('*');
              index += 2;
              continue;
            }

            // закрытие */
            if (index < line.Length - 1 &&
                line[index] == '*' &&
                line[index + 1] == '/' &&
                stack.Peek() == "slash")
            {
              stack.Pop();
              currentComment.Append('/');
              index += 2;

              if (stack.Count == 0)
              {
                comments.Add((commentStartLine,
                              currentComment.ToString().TrimEnd()));
                currentComment.Clear();
                commentStartLine = -1;
              }

              continue;
            }

            // вложенные { }
            if (line[index] == '{')
            {
              stack.Push("brace");
            }
            else if (line[index] == '}' && stack.Peek() == "brace")
            {
              stack.Pop();

              if (stack.Count == 0)
              {
                comments.Add((commentStartLine,
                              currentComment.ToString().TrimEnd()));
                currentComment.Clear();
                commentStartLine = -1;
              }
            }

            index++;
            continue;
          }

          // === вне комментариев ===

          // начало /* */
          if (index < line.Length - 1 &&
              line[index] == '/' &&
              line[index + 1] == '*')
          {
            if (cleanBuilder.Length > 0)
            {
              cleanLines[i] = cleanBuilder.ToString().TrimEnd();
              commentLine = true;
            }

            stack.Push("slash");
            currentComment.Clear();
            currentComment.Append("/*");
            commentStartLine = i;
            index += 2;
            continue;
          }

          // начало { }
          if (line[index] == '{')
          {
            if (cleanBuilder.Length > 0)
            {
              cleanLines[i] = cleanBuilder.ToString().TrimEnd();
              commentLine = true;
            }

            stack.Push("brace");
            currentComment.Clear();
            currentComment.Append('{');
            commentStartLine = i;
            index++;
            continue;
          }

          // начало // комментария
          if (index < line.Length - 1 &&
              line[index] == '/' &&
              line[index + 1] == '/')
          {
            // если перед комментарием был код — сохраняем его
            if (cleanBuilder.Length > 0)
            {
              cleanLines[i] = cleanBuilder.ToString().TrimEnd();
            }

            // сохраняем однострочный комментарий
            string commentText = line.Substring(index).TrimEnd();
            comments.Add((i, commentText));

            // прекращаем обработку строки
            commentLine = true;
            break;
          }

          cleanBuilder.Append(line[index]);
          index++;
        }

        // если вне комментария
        if (stack.Count == 0)
        {
          var cleanLine = cleanBuilder.ToString().TrimEnd();
          if (!string.IsNullOrWhiteSpace(cleanLine) && !commentLine)
            cleanLines[i] = cleanLine;
        }
        else
        {
          currentComment.Append('\n');
        }
      }

      // незакрытый комментарий
      if (stack.Count > 0 && currentComment.Length > 0)
      {
        comments.Add((commentStartLine != -1 ? commentStartLine : lines.Count - 1,
                      currentComment.ToString().TrimEnd()));
      }

      return (cleanLines, comments);
    }
  }
}
