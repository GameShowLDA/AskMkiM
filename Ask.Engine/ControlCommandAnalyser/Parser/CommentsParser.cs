using Ask.Core.Shared.DTO.Executor;
using System.Text;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser
{
  /// <summary>
  /// Выполняет извлечение и удаление комментариев из строк команды.
  /// <para>
  /// Поддерживаются типы комментариев:
  /// <list type="bullet">
  /// <item><description>фигурные: <c>{ ... }</c> (включая двухстрочные);</description></item>
  /// <item><description>C-style: <c>/* ... */</c> (включая двухстрочные);</description></item>
  /// <item><description>однострочные: <c>// ...</c>.</description></item>
  /// </list>
  /// </para>
  /// Найденные комментарии добавляются в коллекцию <see cref="BaseCommandModel.Comment"/>.
  /// </summary>
  public static class CommentsParser
  {
    /// <summary> 
    /// Удаляет комментарии из набора строк и возвращает очищенные строки. 
    /// </summary> 
    /// <param name="lines">Исходные строки команды.</param> 
    /// <param name="model"> 
    /// Модель команды, в которую добавляются найденные комментарии.
    /// </param> 
    /// <returns> 
    /// Список строк без комментариев и без пустых значений.
    /// </returns>
    /// <remarks>
    /// <para><b>Порядок обработки:</b></para>
    /// <list type="number">
    /// <item><description>Фигурные комментарии <c>{…}</c></description></item>
    /// <item><description>Блоки <c>/*…*/</c></description></item>
    /// <item><description>Однострочные <c>//</c></description></item> 
    /// </list>
    /// <para>
    /// Если комментарий занимает две строки, метод корректно изменяет 
    /// содержимое следующей строки, удаляя из неё часть комментария.
    /// </para>
    /// <para> 
    /// Метод не изменяет порядок строк, но может изменить их содержимое. 
    /// </para>
    /// </remarks>
    public static List<string> ParseComments(List<string> lines, BaseCommandModel model)
    {
      if (IsEmpty(lines))
        return new List<string>();

      var result = new List<string>(lines.Count);

      int i = 0;
      while (i < lines.Count)
      {
        if (string.IsNullOrWhiteSpace(lines[i]))
        {
          i++;
          continue;
        }

        var current = new StringBuilder(lines[i]);
        string? nextLine = GetNextLine(lines, i);

        RemoveCommentsFromLine(lines, model, current, ref nextLine, i);

        AddProcessedLine(result, current);

        i++;
      }

      return result;
    }

    /// <summary> 
    /// Проверяет, что список строк пустой или равен <see langword="null"/>.
    /// </summary> 
    /// <param name="lines">Список строк.</param> 
    /// <returns> 
    /// <see langword="true"/>, если список отсутствует или не содержит элементов. 
    /// </returns>
    private static bool IsEmpty(List<string>? lines)
    {
      return lines == null || lines.Count == 0;
    }

    /// <summary> 
    /// Возвращает следующую строку из списка, если она существует. 
    /// </summary> 
    /// <param name="lines">Список строк.</param> 
    /// <param name="index">Текущий индекс.</param>
    /// <returns> 
    /// Следующая строка или <see langword="null"/>, если текущая строка последняя. 
    /// </returns>
    private static string? GetNextLine(List<string> lines, int index)
    {
      return (index + 1 < lines.Count) ? lines[index + 1] : null;
    }

    /// <summary> 
    /// Удаляет комментарии из текущей строки, пока изменения возможны. 
    /// </summary> 
    /// <param name="lines">Список всех строк.</param>
    /// <param name="model">Модель для сохранения комментариев.</param> 
    /// <param name="current">Буфер текущей строки.</param> 
    /// <param name="nextLine">Следующая строка (может быть изменена).</param> 
    /// <param name="index">Индекс текущей строки.</param> 
    /// <remarks> 
    /// Используется цикл до тех пор, пока в строке находятся новые комментарии. 
    /// Это позволяет корректно обработать несколько комментариев в одной строке. 
    /// </remarks>
    private static void RemoveCommentsFromLine(List<string> lines, BaseCommandModel model, StringBuilder current, ref string? nextLine, int index)
    {
      bool changed;
      do
      {
        changed = false;

        if (TryExtractBraceComment(lines, model, current, ref nextLine, index))
        {
          changed = true;
          continue;
        }

        if (TryExtractQuotedComment(lines, model, current, ref nextLine, index))
        {
          changed = true;
          continue;
        }

        if (TryExtractCStyleComment(lines, model, current, ref nextLine, index))
        {
          changed = true;
          continue;
        }

        if (TryExtractLineComment(model, current))
        {
          changed = true;
          continue;
        }

      } while (changed);
    }

    /// <summary> 
    /// Пытается извлечь фигурный комментарий <c>{ … }</c>.
    /// </summary> 
    /// <param name="lines">Список строк.</param> 
    /// <param name="model">Модель для сохранения комментариев.</param> 
    /// <param name="current">Буфер текущей строки.</param> 
    /// <param name="nextLine">Следующая строка.</param> 
    /// <param name="index">Индекс текущей строки.</param> 
    /// <returns> 
    /// <see langword="true"/>, если комментарий найден и удалён. 
    /// </returns> 
    /// <remarks> 
    /// Поддерживаются: 
    /// <list type="bullet"> 
    /// <item><description>однострочные блоки</description></item> 
    /// <item><description>двухстрочные блоки</description></item> 
    /// </list> 
    /// При двухстрочном блоке изменяется следующая строка. 
    /// </remarks>
    private static bool TryExtractBraceComment(List<string> lines, BaseCommandModel model, StringBuilder current, ref string? nextLine, int index)
    {
      int openBrace = current.ToString().IndexOf('{');
      if (openBrace < 0) return false;

      int closeBrace = current.ToString().IndexOf('}', openBrace + 1);

      if (closeBrace >= 0)
      {
        string block = current.ToString().Substring(openBrace, closeBrace - openBrace + 1);
        AddComment(model, block, "{…}");

        current.Remove(openBrace, closeBrace - openBrace + 1);
        return true;
      }

      if (nextLine != null)
      {
        int nextClose = nextLine.IndexOf('}');
        if (nextClose >= 0)
        {
          string thisTail = lines[index].Substring(openBrace);
          string nextHead = nextLine.Substring(0, nextClose + 1);
          string block = thisTail + "\n" + nextHead;

          AddComment(model, block, "{…}(2 строки)");

          current.Remove(openBrace, current.Length - openBrace);
          lines[index + 1] = nextLine.Substring(nextClose + 1);
          nextLine = lines[index + 1];

          return true;
        }
      }

      return false;
    }

    private static bool TryExtractQuotedComment(List<string> lines, BaseCommandModel model, StringBuilder current, ref string? nextLine, int index)
    {
      int open = current.ToString().IndexOf('"');
      if (open < 0) return false;

      int close = current.ToString().IndexOf('"', open + 1);

      // Однострочный
      if (close >= 0)
      {
        string block = current.ToString().Substring(open + 1, close - open - 1);

        AddComment(model, block, "\"...\"");
        current.Remove(open, close - open + 1);

        return true;
      }

      // Двухстрочный
      if (nextLine != null)
      {
        int nextClose = nextLine.IndexOf('"');

        if (nextClose >= 0)
        {
          string thisTail = lines[index].Substring(open).Replace('"','{');
          string nextHead = nextLine.Substring(0, nextClose + 1).Replace('"', '}');

          string block = thisTail + "\n" + nextHead;

          AddComment(model, block, "\"...\" (2 строки)");

          current.Remove(open, current.Length - open);

          lines[index + 1] = nextLine.Substring(nextClose + 1);
          nextLine = lines[index + 1];

          return true;
        }
      }

      return false;
    }

    /// <summary> 
    /// Пытается извлечь C-style комментарий <c>/* … */</c>. 
    /// </summary> 
    /// <param name="lines">Список строк.</param> 
    /// <param name="model">Модель для сохранения комментариев.</param> 
    /// <param name="current">Буфер текущей строки.</param> 
    /// <param name="nextLine">Следующая строка.</param> 
    /// <param name="index">Индекс текущей строки.</param> 
    /// <returns> 
    /// <see langword="true"/>, если комментарий найден. 
    /// </returns> 
    /// <remarks> 
    /// Логика аналогична фигурным комментариям: 
    /// поддерживаются однострочные и двухстрочные блоки. 
    /// </remarks>
    private static bool TryExtractCStyleComment(List<string> lines, BaseCommandModel model, StringBuilder current, ref string? nextLine, int index)
    {
      int openBlock = current.ToString().IndexOf("/*");
      if (openBlock < 0) return false;

      int closeBlock = current.ToString().IndexOf("*/", openBlock + 2);

      if (closeBlock >= 0)
      {
        string block = current.ToString().Substring(openBlock, closeBlock + 2 - openBlock);
        AddComment(model, block, "/*…*/");

        current.Remove(openBlock, closeBlock + 2 - openBlock);
        return true;
      }

      if (nextLine != null)
      {
        int nextClose = nextLine.IndexOf("*/");
        if (nextClose >= 0)
        {
          string thisTail = lines[index].Substring(openBlock);
          string nextHead = nextLine.Substring(0, nextClose + 2);
          string block = thisTail + "\n" + nextHead;

          AddComment(model, block, "/*…*/(2 строки)");

          current.Remove(openBlock, current.Length - openBlock);
          lines[index + 1] = nextLine.Substring(nextClose + 2);
          nextLine = lines[index + 1];

          return true;
        }
      }

      return false;
    }

    /// <summary> 
    /// Пытается извлечь однострочный комментарий <c>// …</c>. 
    /// </summary> 
    /// <param name="model">Модель для сохранения комментариев.</param> 
    /// <param name="current">Буфер текущей строки.</param> 
    /// <returns> 
    /// <see langword="true"/>, если комментарий найден. 
    /// </returns> 
    /// <remarks> 
    /// Используется регулярное выражение для поиска комментария 
    /// до конца строки. 
    /// </remarks>
    private static bool TryExtractLineComment(BaseCommandModel model, StringBuilder current)
    {
      var match = Regex.Match(current.ToString(), @"//.*$");
      if (!match.Success) return false;

      string block = match.Value;
      AddComment(model, block, "//");

      current.Remove(match.Index, current.Length - match.Index);
      return true;
    }

    /// <summary> 
    /// Добавляет комментарий в модель и записывает сообщение в лог. 
    /// </summary> 
    /// <param name="model">Модель команды.</param> 
    /// <param name="block">Текст комментария.</param>
    /// <param name="type">Тип комментария (для логирования).</param> 
    /// <remarks> 
    /// Метод инкапсулирует побочный эффект — запись в лог. 
    /// </remarks>
    private static void AddComment(BaseCommandModel model, string block, string type)
    {
      model.Comment.Add(block);
      LogInformation($"Комментарий {type} найден: {TrimForLog(block)}");
    }

    /// <summary> 
    /// Добавляет очищенную строку в результирующий список. 
    /// </summary> 
    /// <param name="result">Результирующий список.</param>
    /// <param name="current">Буфер текущей строки.</param>
    /// <remarks>
    /// Строка добавляется только если она не пустая после удаления комментариев. 
    /// </remarks>
    private static void AddProcessedLine(List<string> result, StringBuilder current)
    {
      string processed = current.ToString().TrimEnd();
      if (!string.IsNullOrWhiteSpace(processed))
        result.Add(processed);
    }

    /// <summary>
    /// Подготавливает строку комментария для вывода в лог,
    /// экранируя управляющие символы и ограничивая длину.
    /// </summary>
    /// <param name="s">Исходный текст комментария.</param>
    /// <param name="maxLen">Максимальная длина строки в логе.</param>
    /// <returns>Строка, пригодная для безопасного логирования.</returns>
    private static string TrimForLog(string s, int maxLen = 160)
    {
      if (string.IsNullOrEmpty(s)) return s;
      s = s.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
      return s.Length <= maxLen ? s : s.Substring(0, maxLen) + " …";
    }
  }
}
