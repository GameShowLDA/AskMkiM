using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser
{
  public static class CommentsParser
  {
    public static List<string> ParseComments(List<string> lines, BaseCommandModel model)
    {
      if (lines == null || lines.Count == 0)
        return new List<string>();

      var result = new List<string>(lines.Count);

      int i = 0;
      while (i < lines.Count)
      {
        string line = lines[i];
        if (string.IsNullOrWhiteSpace(line))
        {
          i++;
          continue;
        }

        var current = new StringBuilder(line);
        string? nextLine = (i + 1 < lines.Count) ? lines[i + 1] : null;

        bool changed;
        do
        {
          changed = false;

          // 1) Фигурные { ... } (приоритет над /*...*/ если они вложены внутрь) 
          // Пытаемся найти '{' и соответствующую '}' в той же строке
          int openBrace = current.ToString().IndexOf('{');
          if (openBrace >= 0)
          {
            int closeBrace = current.ToString().IndexOf('}', openBrace + 1);

            if (closeBrace >= 0)
            {
              // { ... } в одной строке
              string block = current.ToString().Substring(openBrace, closeBrace - openBrace + 1);
              model.Comment.Add(block);
              LogInformation($"Комментарий {{…}} найден: {TrimForLog(block)}");

              current.Remove(openBrace, closeBrace - openBrace + 1);
              changed = true;
              continue;
            }
            else if (nextLine != null)
            {
              // Попробуем замкнуть на следующей строке
              int nextClose = nextLine.IndexOf('}');
              if (nextClose >= 0)
              {
                string thisTail = lines[i].Substring(openBrace);
                string nextHead = nextLine.Substring(0, nextClose + 1);
                string block = thisTail + "\n" + nextHead;

                model.Comment.Add(block);
                LogInformation($"Комментарий {{…}}(2 строки) найден: {TrimForLog(block)}");

                // Вырезаем из текущей и следующей
                current.Remove(openBrace, current.Length - openBrace);
                lines[i + 1] = nextLine.Substring(nextClose + 1);
                nextLine = lines[i + 1];
                changed = true;
                continue;
              }
            }
          }

          // 2) C-style /* ... */
          int openBlock = current.ToString().IndexOf("/*");
          if (openBlock >= 0)
          {
            int closeBlock = current.ToString().IndexOf("*/", openBlock + 2);
            if (closeBlock >= 0)
            {
              // /* ... */ в одной строке
              string block = current.ToString().Substring(openBlock, closeBlock + 2 - openBlock);
              model.Comment.Add(block);
              LogInformation($"Комментарий (/*…*/) найден: {TrimForLog(block)}");

              current.Remove(openBlock, closeBlock + 2 - openBlock);
              changed = true;
              continue;
            }
            else if (nextLine != null)
            {
              int nextClose = nextLine.IndexOf("*/");
              if (nextClose >= 0)
              {
                string thisTail = lines[i].Substring(openBlock);
                string nextHead = nextLine.Substring(0, nextClose + 2);
                string block = thisTail + "\n" + nextHead;

                model.Comment.Add(block);
                LogInformation($"Комментарий (/*…*/)(2 строки) найден: {TrimForLog(block)}");

                current.Remove(openBlock, current.Length - openBlock);
                lines[i + 1] = nextLine.Substring(nextClose + 2);
                nextLine = lines[i + 1];
                changed = true;
                continue;
              }
            }
          }

          //  3) Линейные // ... (только в текущей строке) 
          // Важно: обрабатываем в конце, чтобы не мешали блокам выше.
          var mLine = Regex.Match(current.ToString(), @"//.*$");
          if (mLine.Success)
          {
            string block = mLine.Value;
            model.Comment.Add(block);
            LogInformation($"Комментарий (//) найден: {TrimForLog(block)}");

            current.Remove(mLine.Index, current.Length - mLine.Index);
            changed = true;
            continue;
          }

        } while (changed); 

        string processedThis = current.ToString().TrimEnd();
        if (!string.IsNullOrWhiteSpace(processedThis))
          result.Add(processedThis);

        // Если мы что-то вырезали из следующей строки (при 2-строчных блоках),
        // lines[i+1] уже обновлена; дальше цикл перейдёт на неё.
        i++;
      }

      return result;
    }

    private static string TrimForLog(string s, int maxLen = 160)
    {
      if (string.IsNullOrEmpty(s)) return s;
      s = s.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
      return s.Length <= maxLen ? s : s.Substring(0, maxLen) + " …";
    }
  }
}
