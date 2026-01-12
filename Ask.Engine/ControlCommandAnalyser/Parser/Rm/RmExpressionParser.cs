using Ask.Core.Services.Errors.Translation;
using Ask.Engine.ControlCommandAnalyser.Model;
using System.Text.RegularExpressions;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Rm
{
  /// <summary>
  /// Класс для парсинга выражений RM (Remote Monitoring) и преобразования их в модели <see cref="RmPairModel"/>.
  /// Поддерживает обработку диапазонов, синонимов, составных выражений и других сложных форматов.
  /// </summary>
  public static class RmExpressionParser
  {
    /// <summary>
    /// Парсит все выражения из блока RM и возвращает список моделей <see cref="RmPairModel"/>.
    /// </summary>
    /// <param name="rmBlock">Текстовый блок с выражениями RM.</param>
    /// <returns>Список моделей <see cref="RmPairModel"/>.</returns>
    public static List<RmPairModel> ParseAllExpressions(string rmBlock, ref RmCommandModel baseCommandModel)
    {
      if (string.IsNullOrEmpty(rmBlock))
      {
        baseCommandModel.Errors.Add(RmErrors.EmptyCommandBody(baseCommandModel.StartLineNumber, $"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}"));
      }

      var expressions = SplitExpressions(rmBlock, ref baseCommandModel);
      var result = new List<RmPairModel>();

      foreach (var expr in expressions)
      {
        ParseExpression(expr, result, ref baseCommandModel);
      }

      return result;
    }

    /// <summary>
    /// Парсит одно выражение и добавляет результаты в список моделей.
    /// </summary>
    /// <param name="expr">Выражение для парсинга.</param>
    /// <param name="result">Список для добавления результатов.</param>
    private static void ParseExpression(string expr, List<RmPairModel> result, ref RmCommandModel baseCommandModel)
    {
      string left, right, middle = null;

      if (TryParseSynonymExpression(expr, out left, out middle, out right))
      {
        ProcessExpression(left, middle, right, result, ref baseCommandModel);
      }
      else if (TryParseBasicExpression(expr, out left, out right))
      {
        ProcessExpression(left, null, right, result, ref baseCommandModel);
      }
      else
      {
        baseCommandModel.Errors.Add(RmErrors.CannotParseExpression(expr, baseCommandModel.StartLineNumber, $"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}"));
      }
    }

    /// <summary>
    /// Пытается распознать выражение с синонимами (формат: "левое == синоним = правое").
    /// </summary>
    /// <param name="expr">Выражение для парсинга.</param>
    /// <param name="left">Левая часть выражения.</param>
    /// <param name="middle">Синоним (средняя часть выражения).</param>
    /// <param name="right">Правая часть выражения.</param>
    /// <returns>True, если выражение успешно распознано; иначе False.</returns>
    private static bool TryParseSynonymExpression(string expr, out string left, out string middle, out string right)
    {
      var synMatch = Regex.Match(expr, @"^(.*?)==\s*(.*?)=(.*)$");
      if (synMatch.Success)
      {
        left = synMatch.Groups[1].Value.Trim();
        middle = synMatch.Groups[2].Value.Trim();
        right = synMatch.Groups[3].Value.Trim();

        // Если средняя часть (middle) пуста, это не синонимное выражение
        if (string.IsNullOrWhiteSpace(middle))
        {
          left = right = middle = null;
          return false;
        }

        return true;
      }

      left = middle = right = null;
      return false;
    }

    /// <summary>
    /// Пытается распознать базовое выражение (формат: "левое = правое").
    /// </summary>
    /// <param name="expr">Выражение для парсинга.</param>
    /// <param name="left">Левая часть выражения.</param>
    /// <param name="right">Правая часть выражения.</param>
    /// <returns>True, если выражение успешно распознано; иначе False.</returns>
    private static bool TryParseBasicExpression(string expr, out string left, out string right)
    {
      var basicMatch = Regex.Match(expr, @"^(.*?)=(.*)$");
      if (basicMatch.Success)
      {
        left = basicMatch.Groups[1].Value.Trim();
        right = basicMatch.Groups[2].Value.Trim();
        return true;
      }

      left = right = null;
      return false;
    }

    /// <summary>
    /// Обрабатывает выражение, расширяет диапазоны и добавляет результаты в список моделей.
    /// </summary>
    /// <param name="left">Левая часть выражения.</param>
    /// <param name="middle">Синоним (средняя часть выражения).</param>
    /// <param name="right">Правая часть выражения.</param>
    /// <param name="result">Список для добавления результатов.</param>
    private static void ProcessExpression(string left, string middle, string right, List<RmPairModel> result, ref RmCommandModel baseCommandModel)
    {
      if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
      {
        baseCommandModel.Errors.Add(RmErrors.EmptyLeftOrRight(left, middle, right, baseCommandModel.StartLineNumber, $"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}"));
        return;
      }

      LogDebug($"[ProcessExpression] Left: {left}, Middle: {middle}, Right: {right}");

      // Новый обработчик для особого случая
      if (TryProcessSynonymRangeWithStep(left, middle, right, result, ref baseCommandModel))
        return;

      if (TryProcessGroupedRanges(left, right, middle, result, ref baseCommandModel))
        return;

      var leftList = ExpandAll(left, ref baseCommandModel);
      var rightList = ExpandAll(right, ref baseCommandModel);
      var middleList = !string.IsNullOrWhiteSpace(middle) ? ExpandAll(middle, ref baseCommandModel) : Enumerable.Repeat<string?>(null, leftList.Count).ToList();

      // Диагностика: выводим размеры списков
      LogDebug($"[ProcessExpression] Expanded Left: {string.Join(", ", leftList)} (Count: {leftList.Count})");
      LogDebug($"[ProcessExpression] Expanded Right: {string.Join(", ", rightList)} (Count: {rightList.Count})");

      // Проверяем соответствие размеров списков
      if (leftList.Count != rightList.Count)
      {
        baseCommandModel.Errors.Add(RmErrors.MismatchedCounts(leftList.Count, rightList.Count, baseCommandModel.StartLineNumber, $"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}"));
        return;
      }

      for (int i = 0; i < leftList.Count; i++)
      {
        result.Add(new RmPairModel
        {
          OkPoint = leftList[i],
          Synonym = middleList[i],
          AskInput = rightList[i]
        });
      }
    }


    /// <summary>
    /// Разделяет текстовый блок на отдельные выражения.
    /// </summary>
    /// <param name="input">Текстовый блок.</param>
    /// <returns>Список выражений.</returns>
    public static List<string> SplitExpressions(string input, ref RmCommandModel baseCommandModel)
    {
      var rawLines = input.Replace("\r", "").Split('\n');
      var result = new List<string>();
      foreach (var line in rawLines)
      {
        if (line.Contains(" "))
        {
          var splitedLines = line.Split(' ');
          foreach (var splitedLine in splitedLines)
          {
            if (!string.IsNullOrEmpty(splitedLine))
            {
              var lineMatches = Regex.Matches(splitedLine, @"[^=]+=[^=]+");
              foreach (Match m in lineMatches)
              {
                var expr = m.Value.Trim();
                if (!string.IsNullOrWhiteSpace(expr) && expr.Contains("=") && !expr.StartsWith("=") && !expr.EndsWith("="))
                  result.Add(expr);
              }
              if (lineMatches.Count == 0)
              {
                baseCommandModel.Errors.Add(RmErrors.ExtraSpace(splitedLine, baseCommandModel.StartLineNumber, $"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}"));
              }
            }
          }
          continue;
        }
        var trimmed = line.Trim(' ');
        if (string.IsNullOrWhiteSpace(trimmed)) continue;
        // Если есть двойное ==, это отдельное выражение
        if (trimmed.Contains("=="))
        {
          result.Add(trimmed);
          continue;
        }
        // Обычные выражения
        var matches = Regex.Matches(trimmed, @"[^=]+=[^=]+");
        foreach (Match m in matches)
        {
          var expr = m.Value.Trim();
          if (!string.IsNullOrWhiteSpace(expr) && expr.Contains("=") && !expr.StartsWith("=") && !expr.EndsWith("="))
            result.Add(expr);
        }
      }
      return result;
    }

    /// <summary>
    /// Расширяет выражение, обрабатывая диапазоны, массивы и составные части.
    /// </summary>
    /// <param name="expr">Выражение для расширения.</param>
    /// <returns>Список расширенных значений.</returns>
    public static List<string> ExpandAll(string expr, ref RmCommandModel baseCommandModel)
    {
      var result = new List<string>();
      expr = expr.Trim();

      LogDebug($"[ExpandAll] Processing expression: {expr}");


      if (expr.Contains('[') || expr.Contains(']') || expr.Contains('"') || expr.Contains('$') || expr.Contains(','))
      {
        baseCommandModel.Errors.Add(RmErrors.UnacceptableSymbol(expr, baseCommandModel.StartLineNumber, $"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}"));
        return result;
      }

      if (TryExpandCompositeRanges(expr, result))
      {
        LogDebug($"[ExpandAll] Composite range expanded: {expr} -> {string.Join(", ", result)}");
        return result;
      }

      if (TryExpandDelimitedRanges(expr, result))
      {
        LogDebug($"[ExpandAll] Delimited range expanded: {expr} -> {string.Join(", ", result)}");
        return result;
      }

      result.Add(expr);
      LogDebug($"[ExpandAll] Single value: {expr}");
      return result;
    }

    /// <summary>
    /// Пытается расширить составные диапазоны (например, "1.1-3.5").
    /// </summary>
    private static bool TryExpandCompositeRanges(string expr, List<string> result)
    {
      var compParts = expr.Split('.');
      if (compParts.Length > 1 && compParts.Any(p => p.Contains('-')))
      {
        if (!string.Equals(compParts.FirstOrDefault(p => p.Contains("-")), compParts.ElementAt(compParts.Length - 1)))
        {
          var dashElements = compParts.FirstOrDefault(p => p.Contains("-")).Split('-');
          if (compParts.ElementAt(0).Equals(dashElements.ElementAt(1)) &&
            compParts.ElementAt(1).Equals(compParts.ElementAt(3)))
          {
            var newExpr = $"{compParts.ElementAt(0)}.{compParts.ElementAt(1)}.{dashElements.ElementAt(0)}-{compParts.ElementAt(4)}";
            compParts = newExpr.Split('.');
          }
        }
        var ranges = compParts.Select(ExpandComponent).ToList();
        foreach (var tuple in CartesianProduct(ranges))
          result.Add(string.Join(".", tuple));
        return true;
      }
      return false;
    }

    /// <summary>
    /// Пытается расширить диапазоны с разделителями (например, "X1/1-3").
    /// </summary>
    private static bool TryExpandDelimitedRanges(string expr, List<string> result)
    {
      var diap2 = Regex.Match(expr, @"^([^\s/=]+/)?([^\s/=]+)-([^\s/=]+)$");
      if (diap2.Success)
      {
        string prefix = diap2.Groups[1].Success ? diap2.Groups[1].Value : "";
        string from = diap2.Groups[2].Value;
        string to = diap2.Groups[3].Value;
        result.AddRange(ExpandRange(prefix, from, to));
        return true;
      }
      return false;
    }

    /// <summary>
    /// Расширяет числовые и буквенные диапазоны.
    /// </summary>
    public static List<string> ExpandRange(string prefix, string from, string to, int step = 1)
    {
      var result = new List<string>();

      if (TryExpandLetterRanges(prefix, from, to, step, result))
        return result;

      if (int.TryParse(from, out int nFrom) && int.TryParse(to, out int nTo))
      {
        int sign = nTo >= nFrom ? 1 : -1;
        for (int n = nFrom; sign > 0 ? n <= nTo : n >= nTo; n += sign * step)
          result.Add($"{prefix}{n}");
      }
      else
      {
        result.Add($"{prefix}{from}-{to}");
      }

      return result;
    }

    /// <summary>
    /// Пытается расширить буквенные диапазоны (например, "A1-A3").
    /// </summary>
    private static bool TryExpandLetterRanges(string prefix, string from, string to, int step, List<string> result)
    {
      var rg = new Regex(@"^([А-ЯA-Z]+)(\d+)$");
      var mFrom = rg.Match(from);
      var mTo = rg.Match(to);
      if (mFrom.Success && mTo.Success)
      {
        var letter = mFrom.Groups[1].Value;
        int nFrom = int.Parse(mFrom.Groups[2].Value);
        int nTo = int.Parse(mTo.Groups[2].Value);
        int sign = nTo >= nFrom ? 1 : -1;
        for (int n = nFrom; sign > 0 ? n <= nTo : n >= nTo; n += sign * step)
          result.Add($"{prefix}{letter}{n}");
        return true;
      }
      return false;
    }

    /// <summary>
    /// Расширяет компонент диапазона (например, "1-3").
    /// </summary>
    public static List<string> ExpandComponent(string part)
    {
      part = part.Trim();
      var diap = Regex.Match(part, @"^([^\s/=]+)-([^\s/=]+)(?:\((\d+)\))?$");
      if (diap.Success)
      {
        string from = diap.Groups[1].Value;
        string to = diap.Groups[2].Value;
        int step = diap.Groups[3].Success ? int.Parse(diap.Groups[3].Value) : 1;
        return ExpandRange("", from, to, step);
      }
      else
        return new List<string> { part };
    }

    /// <summary>
    /// Генерирует декартово произведение списков.
    /// </summary>
    public static IEnumerable<List<string>> CartesianProduct(List<List<string>> sequences)
    {
      IEnumerable<List<string>> result = new List<List<string>> { new List<string>() };
      foreach (var sequence in sequences)
      {
        result = from seq in result
                 from item in sequence
                 select new List<string>(seq) { item };
      }
      return result;
    }

    /// <summary>
    /// Пытается обработать выражение с групповыми диапазонами (например, "101-300=1.[3,5].1-100").
    /// </summary>
    /// <param name="left">Левая часть выражения.</param>
    /// <param name="right">Правая часть выражения.</param>
    /// <param name="middle">Синоним (средняя часть выражения).</param>
    /// <param name="result">Список для добавления результатов.</param>
    /// <returns>True, если выражение успешно обработано; иначе False.</returns>
    private static bool TryProcessGroupedRanges(string left, string right, string middle, List<RmPairModel> result, ref RmCommandModel baseCommandModel)
    {
      // Регулярное выражение для левой части (диапазон чисел, например, "101-300")
      var leftMatch = Regex.Match(left, @"^(\d+)-(\d+)$");
      // Регулярное выражение для правой части (составной диапазон, например, "1.3.1-100")
      var rightMatch = Regex.Match(right, @"^(\d+)\.(\d+)\.(\d+)-(\d+)$");

      if (leftMatch.Success && rightMatch.Success)
      {
        // Парсим левую часть
        int leftFrom = int.Parse(leftMatch.Groups[1].Value);
        int leftTo = int.Parse(leftMatch.Groups[2].Value);
        int leftTotal = leftTo - leftFrom + 1;

        // Парсим правую часть
        int rightPrefix = int.Parse(rightMatch.Groups[1].Value);
        var midList = rightMatch.Groups[2].Value.Split(',').Select(x => int.Parse(x.Trim())).ToList();
        int rightStart = int.Parse(rightMatch.Groups[3].Value);
        int rightEnd = int.Parse(rightMatch.Groups[4].Value);

        // Проверяем, можно ли разбить левую часть на группы
        int groupCount = midList.Count;
        int groupSize = leftTotal / groupCount;
        if (groupSize * groupCount != leftTotal)
        {
          baseCommandModel.Errors.Add(RmErrors.GroupMismatch(baseCommandModel.StartLineNumber, $"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}"));
          return false;
        }

        int leftPtr = leftFrom;
        foreach (var mid in midList)
        {
          for (int i = 0; i < groupSize; i++)
          {
            int rightNum = rightStart + i;
            if (rightNum > rightEnd)
            {
              baseCommandModel.Errors.Add(RmErrors.GroupTooShort(baseCommandModel.StartLineNumber, $"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}"));
              return false;
            }

            result.Add(new RmPairModel
            {
              OkPoint = leftPtr++.ToString(),
              Synonym = middle, // Не поддерживаем синонимы в таких записях (можно расширить при необходимости)
              AskInput = $"{rightPrefix}.{mid}.{rightNum}"
            });
          }
        }
        return true;
      }

      return false;
    }

    /// <summary>
    /// Пытается обработать выражение с синонимами и шагами (например, "301-310 == XS1:1-10 = 1.6.2-20(2)").
    /// </summary>
    /// <param name="left">Левая часть выражения.</param>
    /// <param name="middle">Синоним (средняя часть выражения).</param>
    /// <param name="right">Правая часть выражения.</param>
    /// <param name="result">Список для добавления результатов.</param>
    /// <returns>True, если выражение успешно обработано; иначе False.</returns>
    private static bool TryProcessSynonymRangeWithStep(string left, string middle, string right, List<RmPairModel> result, ref RmCommandModel baseCommandModel)
    {
      // Левый диапазон: 301-310
      var leftMatch = Regex.Match(left, @"^(\d+)-(\d+)$");
      // Синоним: XS1:1-10
      var middleMatch = Regex.Match(middle ?? "", @"^([A-Za-z]+)(\d+):(\d+)-(\d+)$");
      // Правая часть: 1.6.2-20(2)
      var rightMatch = Regex.Match(right, @"^(\d+\.\d+\.)(\d+)-(\d+)\((\d+)\)$");

      if (leftMatch.Success && middleMatch.Success && rightMatch.Success)
      {
        int leftFrom = int.Parse(leftMatch.Groups[1].Value);
        int leftTo = int.Parse(leftMatch.Groups[2].Value);

        string synPrefix = middleMatch.Groups[1].Value;
        int synBase = int.Parse(middleMatch.Groups[2].Value); // XS1
        int synRangeFrom = int.Parse(middleMatch.Groups[3].Value); // 1
        int synRangeTo = int.Parse(middleMatch.Groups[4].Value);   // 10

        string rightPrefix = rightMatch.Groups[1].Value;
        int rightFrom = int.Parse(rightMatch.Groups[2].Value);
        int rightTo = int.Parse(rightMatch.Groups[3].Value);
        int rightStep = int.Parse(rightMatch.Groups[4].Value);

        int count = leftTo - leftFrom + 1;
        if (count != synRangeTo - synRangeFrom + 1 || count != ((rightTo - rightFrom) / rightStep) + 1)
        {
          baseCommandModel.Errors.Add(RmErrors.StepRangeMismatch(baseCommandModel.StartLineNumber, $"{baseCommandModel.CommandNumber} {baseCommandModel.Mnemonic}"));
          return false;
        }

        for (int i = 0; i < count; i++)
        {
          int leftVal = leftFrom + i;
          int synNum = synRangeFrom + i;
          int rightVal = rightFrom + (i * rightStep);

          result.Add(new RmPairModel
          {
            OkPoint = leftVal.ToString(),
            Synonym = null,
            AskInput = $"{rightPrefix}{rightVal}"
          });
          result.Add(new RmPairModel
          {
            OkPoint = $"{synPrefix}{synBase}:{synNum}",
            Synonym = null,
            AskInput = $"{rightPrefix}{rightVal}"
          });
        }
        return true;
      }
      return false;
    }
  }
}
