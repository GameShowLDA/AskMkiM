using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Parser
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text.RegularExpressions;
  using ControlCommandAnalyser.Model;

  public static class RmExpressionParser
  {
    public static List<RmPairModel> ParseAllExpressions(string rmBlock)
    {
      // Разбить на выражения (по пробелу, табу, переводу строки)
      var expressions = Regex.Split(rmBlock, @"(?<=[^=])\s+").Where(x => !string.IsNullOrWhiteSpace(x));
      var result = new List<RmPairModel>();

      foreach (var expr in expressions)
      {
        // Парсим: левые == синонимы = правые    или   левые = правые
        // Поддержка факультативных синонимов
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

        var leftList = ExpandAll(left);
        var rightList = ExpandAll(right);
        var middleList = !string.IsNullOrWhiteSpace(middle) ? ExpandAll(middle) : null;

        // Проверка количества элементов
        if ((middleList != null && (leftList.Count != rightList.Count || leftList.Count != middleList.Count))
            || (middleList == null && leftList.Count != rightList.Count))
          throw new Exception("Количество точек ОК, синонимов и входов должно совпадать!\n" + expr);

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

    // Разворачивает сложную часть (с диапазонами, массивами и т.д.) в список точек/входов
    public static List<string> ExpandAll(string expr)
    {
      var result = new List<string>();
      expr = expr.Trim();

      // [X20,X30]/1 = 1.2.[100,98] поддержка скобок для массивов
      // Сначала парсим массивы через [ ... ]
      var bracketRegex = new Regex(@"\[([^\[\]]+)\]");
      var match = bracketRegex.Match(expr);
      if (match.Success)
      {
        // Для "[X20,X30]/1" -> ("X20","X30") + "/1"
        var items = match.Groups[1].Value.Split(',').Select(s => s.Trim()).ToList();
        string suffix = expr.Substring(match.Index + match.Length); // всё после скобки
        foreach (var item in items)
          result.Add(item + suffix);
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
      var diap = Regex.Match(expr, @"^([^\s/=]+/)?([^\s/=]+)-([^\s/=]+)$");
      if (diap.Success)
      {
        // Префикс, например "Х1/" (или пусто)
        string prefix = diap.Groups[1].Success ? diap.Groups[1].Value : "";
        string from = diap.Groups[2].Value;
        string to = diap.Groups[3].Value;
        result.AddRange(ExpandRange(prefix, from, to));
        return result;
      }

      // Составные диапазоны типа 1.[3-5].1-100
      // Делим по точке, ищем диапазоны в каждом компоненте
      var compParts = expr.Split('.');
      if (compParts.Any(p => p.Contains('-')))
      {
        // Рекурсивно строим все варианты для составных диапазонов
        var ranges = compParts.Select(ExpandComponent).ToList();
        foreach (var tuple in CartesianProduct(ranges))
          result.Add(string.Join(".", tuple));
        return result;
      }

      // Просто число или просто слово
      result.Add(expr);
      return result;
    }

    // Разворачивает диапазон от from до to (например, "A1-A30" или "1-10")
    public static List<string> ExpandRange(string prefix, string from, string to)
    {
      var result = new List<string>();
      if (int.TryParse(from, out int nFrom) && int.TryParse(to, out int nTo))
      {
        int sign = nTo >= nFrom ? 1 : -1;
        for (int n = nFrom; sign > 0 ? n <= nTo : n >= nTo; n += sign)
          result.Add($"{prefix}{n}");
      }
      else
      {
        // Диапазон с буквами (например, А1-А30, Б1-Б31)
        // Ожидаем формат Б1-Б31 (буква+число)
        var rg = new Regex(@"^([А-ЯA-Z]+)(\d+)$");
        var mFrom = rg.Match(from);
        var mTo = rg.Match(to);
        if (mFrom.Success && mTo.Success)
        {
          var letter = mFrom.Groups[1].Value;
          int nFrom2 = int.Parse(mFrom.Groups[2].Value);
          int nTo2 = int.Parse(mTo.Groups[2].Value);
          int sign = nTo2 >= nFrom2 ? 1 : -1;
          for (int n = nFrom2; sign > 0 ? n <= nTo2 : n >= nTo2; n += sign)
            result.Add($"{prefix}{letter}{n}");
        }
        else
        {
          // Не удалось разобрать — возвращаем как есть
          result.Add($"{prefix}{from}-{to}");
        }
      }
      return result;
    }

    // Компонент составного диапазона: либо просто элемент, либо диапазон
    public static List<string> ExpandComponent(string part)
    {
      part = part.Trim();
      var diap = Regex.Match(part, @"^([^\s/=]+)-([^\s/=]+)$");
      if (diap.Success)
        return ExpandRange("", diap.Groups[1].Value, diap.Groups[2].Value);
      else
        return new List<string> { part };
    }

    // Декартово произведение списков (для составных диапазонов)
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
