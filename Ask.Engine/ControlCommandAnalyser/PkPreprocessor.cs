namespace Ask.Engine.ControlCommandAnalyser
{
  public static class PkPreprocessor
  {
    private const int MaxShortCommentLines = 2; // 1–2 строки оставляем, >2 — удаляем

    public static string PreprocessText(string text)
    {
      if (string.IsNullOrEmpty(text))
        return text;

      text = text.Replace("\r\n", "\n");

      var sb = new System.Text.StringBuilder();
      var stack = new Stack<(string type, int start)>();
      int i = 0;
      int lastAppended = 0;

      while (i < text.Length)
      {
        // --- открытие /* ---
        if (i + 1 < text.Length && text[i] == '/' && text[i + 1] == '*')
        {
          // если не находимся внутри { }, считаем это началом блочного комментария
          if (!IsInside(stack, "brace"))
          {
            stack.Push(("slash", i));
            // добавить текст до начала комментария
            sb.Append(text.Substring(lastAppended, i - lastAppended));
            i += 2;
            continue;
          }
        }

        // --- закрытие */ ---
        if (i + 1 < text.Length && text[i] == '*' && text[i + 1] == '/')
        {
          if (stack.Count > 0 && stack.Peek().type == "slash")
          {
            var start = stack.Pop().start;
            var comment = text.Substring(start, i + 2 - start);
            int lineCount = CountLines(comment);
            sb.Append(new string('\n', Math.Max(1, lineCount - 1)));
            i += 2;
            lastAppended = i;
            continue;
          }
        }

        // --- открытие { ---
        if (text[i] == '{')
        {
          // открываем только если не внутри /* */
          if (!IsInside(stack, "slash"))
          {
            stack.Push(("brace", i));
          }
          i++;
          continue;
        }

        // --- закрытие } ---
        if (text[i] == '}')
        {
          if (stack.Count > 0 && stack.Peek().type == "brace")
          {
            var start = stack.Pop().start;
            var block = text.Substring(start, i + 1 - start);
            int lineCount = CountLines(block);

            // Если блок большой — удаляем, иначе оставляем
            if (lineCount > MaxShortCommentLines)
            {
              sb.Append(new string('\n', Math.Max(1, lineCount - 1)));
              i++;
              lastAppended = i;
              continue;
            }
            else
            {
              // оставляем текст блока
              sb.Append(block);
              i++;
              lastAppended = i;
              continue;
            }
          }
          i++;
          continue;
        }

        // --- однострочные комментарии // ---
        if (i + 1 < text.Length && text[i] == '/' && text[i + 1] == '/')
        {
          // добавляем текст до комментария
          sb.Append(text.Substring(lastAppended, i - lastAppended));
          int end = text.IndexOf('\n', i);
          if (end == -1)
            end = text.Length;

          // заменяем весь комментарий на пустую строку
          sb.Append('\n');
          i = end + 1;
          lastAppended = i;
          continue;
        }

        i++;
      }

      // Добавляем остаток текста, если вне комментариев
      if (lastAppended < text.Length)
        sb.Append(text.Substring(lastAppended));

      // закрытые, но не завершённые блоки (например { без }) удаляем до конца
      while (stack.Count > 0)
      {
        var open = stack.Pop();
        int lineCount = CountLines(text.Substring(open.start));
        sb.Append(new string('\n', Math.Max(1, lineCount - 1)));
      }

      return sb.ToString();
    }

    /// <summary>
    /// Проверяет, находимся ли мы внутри комментария указанного типа.
    /// </summary>
    private static bool IsInside(Stack<(string type, int start)> stack, string type)
    {
      foreach (var s in stack)
        if (s.type == type)
          return true;
      return false;
    }

    /// <summary>
    /// Подсчёт строк в тексте.
    /// </summary>
    private static int CountLines(string s)
    {
      int count = 1;
      foreach (char c in s)
        if (c == '\n') count++;
      return count;
    }

  }
}
