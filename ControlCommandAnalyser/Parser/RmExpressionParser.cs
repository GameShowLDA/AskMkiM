using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ControlCommandAnalyser.Model;

namespace ControlCommandAnalyser.Parser
{
  public class RmPairModel
  {
    public string OkPoint { get; set; }
    public string? Synonym { get; set; }
    public string AskInput { get; set; }
  }

  public static class RmExpressionParser
  {
    public static List<RmPairModel> ParseAllExpressions(string rmBlock)
    {
      var expressions = SplitExpressions(rmBlock);
      var result = new List<RmPairModel>();

      foreach (var expr in expressions)
      {
        // Парсим: левые == синонимы = правые    или   левые = правые
        string left, right, middle = null;
        var synMatch = Regex.Match(expr, @"^(.*?)==\s*(.*?)=(.*)$");
        if (synMatch.Success)
        {
          left = synMatch.Groups[1].Value.Trim();
          middle = synMatch.Groups[2].Value.Trim();
          right = synMatch.Groups[3].Value.Trim();
        }
        else
        {
          var basicMatch = Regex.Match(expr, @"^(.*?)=(.*)$");
          if (!basicMatch.Success) continue;
          left = basicMatch.Groups[1].Value.Trim();
          right = basicMatch.Groups[2].Value.Trim();
        }

        // Бракуем если левая или правая часть пуста
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
          continue;

        // ==== СПЕЦОБРАБОТКА ГРУППОВЫХ ДИАПАЗОНОВ ====
        // Левый диапазон: a-b (например, 101-300)
        var leftMatch = Regex.Match(left, @"^(\d+)-(\d+)$");
        // Правый составной: X.[a,b].Y-Z (например, 1.[3,5].1-100)
        var rightMatch = Regex.Match(right, @"^(\d+)\.\[([^\]]+)\]\.(\d+)-(\d+)$");
        if (leftMatch.Success && rightMatch.Success)
        {
          int leftFrom = int.Parse(leftMatch.Groups[1].Value);
          int leftTo = int.Parse(leftMatch.Groups[2].Value);
          int leftTotal = leftTo - leftFrom + 1;

          int rightPrefix = int.Parse(rightMatch.Groups[1].Value);
          var midList = rightMatch.Groups[2].Value.Split(',').Select(x => int.Parse(x.Trim())).ToList();
          int rightStart = int.Parse(rightMatch.Groups[3].Value);
          int rightEnd = int.Parse(rightMatch.Groups[4].Value);

          int groupCount = midList.Count;
          int groupSize = leftTotal / groupCount;
          if (groupSize * groupCount != leftTotal)
            throw new Exception("Нельзя разбить диапазон слева на равные группы под массив справа!");

          int leftPtr = leftFrom;
          foreach (var mid in midList)
          {
            for (int i = 0; i < groupSize; i++)
            {
              int rightNum = rightStart + i;
              if (rightNum > rightEnd)
                throw new Exception("Правая группа короче, чем левая!");

              result.Add(new RmPairModel
              {
                OkPoint = (leftPtr++).ToString(),
                Synonym = middle, // Не поддерживаем синонимы в таких записях (можно расширить при необходимости)
                AskInput = $"{rightPrefix}.{mid}.{rightNum}"
              });
            }
          }
          continue; // Всё, обработано!
        }
        // ==== / СПЕЦОБРАБОТКА ====

        // Обычная логика:
        var leftList = ExpandAll(left);
        var rightList = ExpandAll(right);
        var middleList = !string.IsNullOrWhiteSpace(middle) ? ExpandAll(middle) : null;

        if ((middleList != null && (leftList.Count != rightList.Count || leftList.Count != middleList.Count))
            || (middleList == null && leftList.Count != rightList.Count))
          throw new Exception($"Количество точек ОК, синонимов и входов должно совпадать!\n{expr}");

        for (int i = 0; i < leftList.Count; i++)
        {
          result.Add(new RmPairModel
          {
            OkPoint = leftList[i],
            Synonym = middleList != null ? middleList[i] : null,
            AskInput = rightList[i]
          });
        }
      }
      return result;
    }

    public static List<string> SplitExpressions(string input)
    {
      var rawLines = input.Replace("\r", "").Split('\n');
      var result = new List<string>();
      foreach (var line in rawLines)
      {
        var matches = Regex.Matches(line, @"[^=]+=[^=]+");
        foreach (Match m in matches)
        {
          var expr = m.Value.Trim();
          if (!string.IsNullOrWhiteSpace(expr) && expr.Contains("=") && !expr.StartsWith("=") && !expr.EndsWith("="))
            result.Add(expr);
        }
      }
      return result;
    }

    // Все остальные вспомогательные методы (ExpandAll, ExpandRange, ExpandComponent, CartesianProduct) — точно такие же, как выше!
    // ...
    public static List<string> ExpandAll(string expr)
    {
      var result = new List<string>();
      expr = expr.Trim();

      // Если точка внутри диапазона (например, 1.6.2-20(2)), парсим составные!
      var compParts = expr.Split('.');
      if (compParts.Length > 1 && compParts.Any(p => p.Contains('-')))
      {
        var ranges = compParts.Select(ExpandComponent).ToList();
        foreach (var tuple in CartesianProduct(ranges))
          result.Add(string.Join(".", tuple));
        return result;
      }

      // Обработка диапазона формата 1.1.60-78
      var dotRange = Regex.Match(expr, @"^([\d\.]+)\-(\d+)$");
      if (dotRange.Success && expr.Contains('.'))
      {
        var prefix = dotRange.Groups[1].Value;
        var prefixParts = prefix.Split('.').ToList();
        int from = int.Parse(prefixParts.Last());
        int to = int.Parse(dotRange.Groups[2].Value);
        prefixParts.RemoveAt(prefixParts.Count - 1);
        var prefixStr = string.Join(".", prefixParts);

        int sign = to >= from ? 1 : -1;
        for (int n = from; sign > 0 ? n <= to : n >= to; n += sign)
          result.Add($"{prefixStr}.{n}");
        return result;
      }

      // Массив в скобках: [a,b,c] или [3-5]
      var bracketRegex = new Regex(@"\[([^\[\]]+)\]");
      var match = bracketRegex.Match(expr);
      if (match.Success)
      {
        string suffix = expr.Substring(match.Index + match.Length);
        var itemsRaw = match.Groups[1].Value.Split(',').Select(s => s.Trim());
        foreach (var item in itemsRaw)
        {
          // Диапазон внутри скобок (например, [3-5])
          var diap = Regex.Match(item, @"^(\d+)-(\d+)$");
          if (diap.Success)
          {
            int from = int.Parse(diap.Groups[1].Value);
            int to = int.Parse(diap.Groups[2].Value);
            int sign = to >= from ? 1 : -1;
            for (int n = from; sign > 0 ? n <= to : n >= to; n += sign)
              result.Add(n.ToString() + suffix);
          }
          else
          {
            result.Add(item + suffix);
          }
        }
        return result;
      }

      // Парсим запятые на верхнем уровне
      var parts = expr.Split(',').Select(s => s.Trim()).ToList();
      if (parts.Count > 1)
      {
        foreach (var part in parts)
          result.AddRange(ExpandAll(part));
        return result;
      }

      // Диапазоны с "буква/от-до" (например, Х1/А1-А30)
      var diap2 = Regex.Match(expr, @"^([^\s/=]+/)?([^\s/=]+)-([^\s/=]+)$");
      if (diap2.Success)
      {
        string prefix = diap2.Groups[1].Success ? diap2.Groups[1].Value : "";
        string from = diap2.Groups[2].Value;
        string to = diap2.Groups[3].Value;
        result.AddRange(ExpandRange(prefix, from, to));
        return result;
      }

      // Просто число или просто слово
      result.Add(expr);
      return result;
    }

    public static List<string> ExpandRange(string prefix, string from, string to)
    {
      var result = new List<string>();
      // Проверка на шаг: формат "N-M(Step)"
      var mStep = Regex.Match(to, @"^(\d+)\((\d+)\)$");
      int step = 1;
      string toValue = to;
      if (mStep.Success)
      {
        toValue = mStep.Groups[1].Value;
        step = int.Parse(mStep.Groups[2].Value);
      }

      // Новый блок: поддержка префикса с двоеточием!
      // from: XS1:1, to: 10
      var preDiap = Regex.Match(from, @"^(.*:)?(\d+)$");
      if (preDiap.Success)
      {
        string pre = preDiap.Groups[1].Success ? preDiap.Groups[1].Value : "";
        string fromNum = preDiap.Groups[2].Value;
        if (int.TryParse(fromNum, out int nFrom) && int.TryParse(toValue, out int nTo))
        {
          int sign = nTo >= nFrom ? 1 : -1;
          for (int n = nFrom; sign > 0 ? n <= nTo : n >= nTo; n += sign * step)
            result.Add($"{pre}{n}");
          return result;
        }
      }

      // ...дальше твоя стандартная логика...
      if (int.TryParse(from, out int nFrom2) && int.TryParse(toValue, out int nTo2))
      {
        int sign = nTo2 >= nFrom2 ? 1 : -1;
        for (int n = nFrom2; sign > 0 ? n <= nTo2 : n >= nTo2; n += sign * step)
          result.Add($"{prefix}{n}");
      }
      else
      {
        // Буквенные диапазоны (оставь как было)
        var rg = new Regex(@"^([А-ЯA-Z]+)(\d+)$");
        var mFrom2 = rg.Match(from);
        var mTo2 = rg.Match(toValue);
        if (mFrom2.Success && mTo2.Success)
        {
          var letter = mFrom2.Groups[1].Value;
          int nFrom3 = int.Parse(mFrom2.Groups[2].Value);
          int nTo3 = int.Parse(mTo2.Groups[2].Value);
          int sign = nTo3 >= nFrom3 ? 1 : -1;
          for (int n = nFrom3; sign > 0 ? n <= nTo3 : n >= nTo3; n += sign * step)
            result.Add($"{prefix}{letter}{n}");
        }
        else
        {
          result.Add($"{prefix}{from}-{to}");
        }
      }
      return result;
    }



    public static List<string> ExpandComponent(string part)
    {
      part = part.Trim();
      // Поддержка шага (N-M(Step)) внутри компонента!
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

    public static List<string> ExpandRange(string prefix, string from, string to, int step = 1)
    {
      var result = new List<string>();

      // Поддержка префикса с двоеточием!
      var preDiap = Regex.Match(from, @"^(.*:)?(\d+)$");
      if (preDiap.Success)
      {
        string pre = preDiap.Groups[1].Success ? preDiap.Groups[1].Value : "";
        string fromNum = preDiap.Groups[2].Value;
        // Парсим to — если вдруг to содержит скобки (шаг), отделяем число
        var mTo = Regex.Match(to, @"^(\d+)(?:\((\d+)\))?$");
        string toNum = mTo.Success ? mTo.Groups[1].Value : to;
        int mStep = mTo.Success && mTo.Groups[2].Success ? int.Parse(mTo.Groups[2].Value) : step;
        if (int.TryParse(fromNum, out int nFrom) && int.TryParse(toNum, out int nTo))
        {
          int sign = nTo >= nFrom ? 1 : -1;
          for (int n = nFrom; sign > 0 ? n <= nTo : n >= nTo; n += sign * mStep)
            result.Add($"{pre}{n}");
          return result;
        }
      }

      // Просто числовой диапазон
      if (int.TryParse(from, out int nFrom2) && int.TryParse(to, out int nTo2))
      {
        int sign = nTo2 >= nFrom2 ? 1 : -1;
        for (int n = nFrom2; sign > 0 ? n <= nTo2 : n >= nTo2; n += sign * step)
          result.Add($"{prefix}{n}");
      }
      else
      {
        // Буквенные диапазоны
        var rg = new Regex(@"^([А-ЯA-Z]+)(\d+)$");
        var mFrom2 = rg.Match(from);
        var mTo2 = rg.Match(to);
        if (mFrom2.Success && mTo2.Success)
        {
          var letter = mFrom2.Groups[1].Value;
          int nFrom3 = int.Parse(mFrom2.Groups[2].Value);
          int nTo3 = int.Parse(mTo2.Groups[2].Value);
          int sign = nTo3 >= nFrom3 ? 1 : -1;
          for (int n = nFrom3; sign > 0 ? n <= nTo3 : n >= nTo3; n += sign * step)
            result.Add($"{prefix}{letter}{n}");
        }
        else
        {
          result.Add($"{prefix}{from}-{to}");
        }
      }
      return result;
    }

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
  }
}
