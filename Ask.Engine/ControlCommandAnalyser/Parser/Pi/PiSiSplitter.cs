using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using System.Text.RegularExpressions;

namespace Ask.Engine.ControlCommandAnalyser.Parser.Pi
{
  /// <summary>
  /// Утилита для разделения строки параметров команды ПИ на части СИ и ПИ.
  /// Выполняет лексический разбор, поиск точки разделения и нормализацию текста.
  /// </summary>
  public static partial class PiSiSplitter
  {
    private static readonly HashSet<string> SiKeys = new(StringComparer.OrdinalIgnoreCase)
    {AlgorithmKey.К.ToString(),
     AlgorithmKey.С.ToString(),
     AlgorithmKey.П.ToString(),
     AlgorithmKey.И.ToString(),
     AlgorithmKey.Г.ToString(),
     AlgorithmKey.Т1.ToString() };

    private enum TokType { Volt, Time, Res, Key, Points, Comma, Ws, Other }

    private readonly record struct Tok(TokType Type, string Text, int Start, int Length)
    {
      public int End => Start + Length;
    }

    /// <summary>
    /// Разделяет строку параметров на части СИ и ПИ,
    /// выполняя токенизацию и выбор оптимальной точки разреза.
    /// </summary>
    /// <param name="input">Исходная строка параметров.</param>
    /// <returns>Кортеж: часть СИ и часть ПИ.</returns>
    public static (string SiPart, string PiPart) SplitSiFromPi(string input)
    {
      if (string.IsNullOrWhiteSpace(input))
        return ("", "");

      var toks = Lex(input);

      var candidates = new List<(int idx, int score)>();
      for (int i = 0; i <= toks.Count; i++)
      {
        var left = toks.Take(i).ToList();
        var right = toks.Skip(i).ToList();

        if (!IsValidSi(left)) continue;
        if (!IsValidPi(right)) continue;

        int score = 0;
        // правой части с точками даём большой бонус
        if (right.Any(t => t.Type == TokType.Points)) score += 100;
        // широкий пробел/таб между частями
        if (IsBigWhitespaceBoundary(input, left, right)) score += 20;
        // предпочитаем ПОЗДНИЙ разрез (длинную СИ)
        score += i;

        candidates.Add((i, score));
      }

      // fallback-кандидат: разрез перед первым «правым» вольтажом
      if (candidates.Count == 0)
      {
        int firstRightVolt = toks.FindIndex(t => t.Type == TokType.Volt);
        if (firstRightVolt > 0 && firstRightVolt <= toks.Count - 1)
        {
          var lft = toks.Take(firstRightVolt).ToList();
          var rgt = toks.Skip(firstRightVolt).ToList();
          if (IsValidSi(lft) && IsValidPi(rgt))
            candidates.Add((firstRightVolt, 1));
        }
      }

      if (candidates.Count == 0)
      {
        // совсем безопасный вариант: всё — СИ
        return (NormalizeSi(input), "");
      }

      var best = candidates
        .OrderByDescending(c => c.score)
        .ThenBy(c => c.idx)
        .First();

      var siStr = Reconstruct(input, toks.Take(best.idx));
      var piStr = Reconstruct(input, toks.Skip(best.idx));

      return (NormalizeSi(siStr), piStr.Trim());
    }

    /// <summary>
    /// Проверяет, может ли набор токенов быть корректной частью СИ.
    /// </summary>
    /// <param name="toks">Список токенов.</param>
    /// <returns>true, если структура СИ валидна.</returns>
    private static bool IsValidSi(List<Tok> toks)
    {
      if (toks.Count == 0) return true;

      int volt = 0, time = 0, res = 0, keys = 0;
      foreach (var t in toks)
      {
        switch (t.Type)
        {
          case TokType.Points: return false;
          case TokType.Volt: volt++; break;
          case TokType.Time: time++; break;
          case TokType.Res: res++; break;
          case TokType.Key:
            if (!IsSiKey(t.Text)) return false;
            keys++; break;
          case TokType.Other: return false;
        }
        if (volt > 1 || time > 1 || res > 1) return false;
      }

      return (volt + time + res + keys) > 0;
    }

    /// <summary>
    /// Проверяет, может ли набор токенов быть корректной частью ПИ.
    /// </summary>
    /// <param name="toks">Список токенов.</param>
    /// <returns>true, если структура ПИ валидна.</returns>
    private static bool IsValidPi(List<Tok> toks)
    {
      if (toks.Count == 0) return false;

      int volt = 0, time = 0;
      foreach (var t in toks)
      {
        switch (t.Type)
        {
          case TokType.Volt: volt++; break;
          case TokType.Time: time++; break;
          case TokType.Points: break; // ок
          case TokType.Key: break;    // ок
          case TokType.Res: return false; // сопротивление в ПИ запрещаем по правилам
          case TokType.Other: return false;
        }
        if (time > 1) return false;
      }

      return volt >= 1;
    }

    /// <summary>
    /// Проверяет, является ли ключ допустимым ключом СИ.
    /// </summary>
    /// <param name="k">Текст ключа.</param>
    /// <returns>true, если ключ относится к СИ.</returns>
    private static bool IsSiKey(string k) => SiKeys.Contains(k);

    /// <summary>
    /// Выполняет лексический разбор строки на токены.
    /// </summary>
    /// <param name="s">Исходная строка.</param>
    /// <returns>Список токенов.</returns>
    private static List<Tok> Lex(string s)
    {
      var toks = new List<Tok>();
      int i = 0;
      while (i < s.Length)
      {
        if (char.IsWhiteSpace(s[i]))
        {
          int j = i + 1;
          while (j < s.Length && char.IsWhiteSpace(s[j])) j++;
          toks.Add(new Tok(TokType.Ws, s[i..j], i, j - i));
          i = j;
          continue;
        }

        if (s[i] == ',')
        {
          toks.Add(new Tok(TokType.Comma, ",", i, 1));
          i++;
          continue;
        }

        if (s[i] == '*')
        {
          int j = i + 1;
          while (j < s.Length && s[j] != '\n' && !char.IsWhiteSpace(s[j])) j++;
          toks.Add(new Tok(TokType.Points, s[i..j], i, j - i));
          i = j;
          continue;
        }

        {
          var m = Regex.Match(s.Substring(i), @"^[\+\-]?\d+\s*В", RegexOptions.IgnoreCase);
          if (m.Success)
          {
            toks.Add(new Tok(TokType.Volt, m.Value.Trim(), i, m.Length));
            i += m.Length;
            continue;
          }
        }

        {
          var m = Regex.Match(s.Substring(i), @"^\d+\s*С", RegexOptions.IgnoreCase);
          if (m.Success)
          {
            toks.Add(new Tok(TokType.Time, m.Value.Trim(), i, m.Length));
            i += m.Length;
            continue;
          }
        }

        {
          var m = Regex.Match(s.Substring(i), @"^\d+\s*<\s*(МОМ|МОм|кОм|ГОм)", RegexOptions.IgnoreCase);
          if (m.Success)
          {
            toks.Add(new Tok(TokType.Res, NormalizeRes(m.Value), i, m.Length));
            i += m.Length;
            continue;
          }
        }

        {
          var mT1 = Regex.Match(s.Substring(i), @"^(?:T|Т)1(?=$|[\s,])", RegexOptions.IgnoreCase);
          if (mT1.Success)
          {
            toks.Add(new Tok(TokType.Key, "Т1", i, mT1.Length));
            i += mT1.Length;
            continue;
          }

          var mKey = Regex.Match(s.Substring(i), @"^[ПСКДИГ](?=$|[\s,])", RegexOptions.IgnoreCase);
          if (mKey.Success)
          {
            toks.Add(new Tok(TokType.Key, mKey.Value.ToUpperInvariant(), i, mKey.Length));
            i += mKey.Length;
            continue;
          }
        }

        // копим до ближайшего разделителя
        int k = i + 1;
        while (k < s.Length && !char.IsWhiteSpace(s[k]) && s[k] != ',' && s[k] != '*') k++;
        toks.Add(new Tok(TokType.Other, s[i..k], i, k - i));
        i = k;
      }

      // отфильтруем чистые разделители
      return toks.Where(t => t.Type is not TokType.Ws and not TokType.Comma).ToList();
    }

    /// <summary>
    /// Нормализует запись сопротивления (приводит единицы и формат).
    /// </summary>
    /// <param name="text">Исходный текст.</param>
    /// <returns>Нормализованная строка сопротивления.</returns>
    private static string NormalizeRes(string text)
    {
      var t = Regex.Replace(text, @"\s+", "");
      t = Regex.Replace(t, "МОМ", "МОм", RegexOptions.IgnoreCase);
      return t;
    }

    /// <summary>
    /// Определяет, есть ли между частями строки «широкая» граница (много пробелов или таб).
    /// </summary>
    /// <param name="input">Исходная строка.</param>
    /// <param name="left">Левая часть токенов.</param>
    /// <param name="right">Правая часть токенов.</param>
    /// <returns>true, если граница считается значимой.</returns>
    private static bool IsBigWhitespaceBoundary(string input, List<Tok> left, List<Tok> right)
    {
      if (left.Count == 0 || right.Count == 0) return false;

      int leftEnd = left[^1].End;
      int rightBeg = right[0].Start;
      if (leftEnd >= rightBeg || rightBeg > input.Length) return false;

      bool hasTab = false; int spaces = 0;
      for (int j = leftEnd; j < rightBeg; j++)
      {
        char c = input[j];
        if (c == '\t') hasTab = true;
        if (c == ' ') spaces++;
        if (!char.IsWhiteSpace(c)) return false;
      }
      return hasTab || spaces >= 2;
    }

    /// <summary>
    /// Восстанавливает подстроку исходного текста по диапазону токенов.
    /// </summary>
    /// <param name="input">Исходная строка.</param>
    /// <param name="tokens">Токены.</param>
    /// <returns>Подстрока исходного текста.</returns>
    private static string Reconstruct(string input, IEnumerable<Tok> tokens)
    {
      var list = tokens.ToList();
      if (list.Count == 0) return "";
      int from = list.First().Start;
      int to = list.Last().End;
      return input.Substring(from, to - from);
    }

    /// <summary>
    /// Выполняет предварительную нормализацию строки:
    /// заменяет специальные пробелы и латинские аналоги букв.
    /// </summary>
    /// <param name="s">Исходная строка.</param>
    /// <returns>Нормализованная строка.</returns>
    public static string PreNormalize(string s)
    {
      if (string.IsNullOrEmpty(s)) return s;

      // NBSP и «узкие» пробелы -> обычный пробел
      s = s.Replace('\u00A0', ' ')  // NBSP
           .Replace('\u2007', ' ')  // Figure Space
           .Replace('\u202F', ' '); // Narrow NBSP

      // Привести «похожих» латинских букв к кириллице (B->В, C->С, H->Н и т.д.)
      var map = new Dictionary<char, char>
      {
        ['A'] = 'А',
        ['a'] = 'а',
        ['B'] = 'В',
        ['b'] = 'в',
        ['C'] = 'С',
        ['c'] = 'с',
        ['E'] = 'Е',
        ['e'] = 'е',
        ['H'] = 'Н',
        ['h'] = 'н',
        ['K'] = 'К',
        ['k'] = 'к',
        ['M'] = 'М',
        ['m'] = 'м',
        ['O'] = 'О',
        ['o'] = 'о',
        ['P'] = 'Р',
        ['p'] = 'р',
        ['T'] = 'Т',
        ['t'] = 'т',
        ['X'] = 'Х',
        ['x'] = 'х',
        ['Y'] = 'У',
        ['y'] = 'у'
      };

      var arr = s.ToCharArray();
      for (int i = 0; i < arr.Length; i++)
        if (map.TryGetValue(arr[i], out var ru)) arr[i] = ru;

      return new string(arr);
    }
  }

  /// <summary>
  /// Расширенные методы разделения ПИ/СИ с диагностикой ошибок.
  /// </summary>
  public static partial class PiSiSplitter
  {
    /// <summary>
    /// Строго разделяет строку на части СИ и ПИ,
    /// возвращая диагностические ошибки при нарушении структуры.
    /// </summary>
    /// <param name="input">Исходная строка.</param>
    /// <returns>Кортеж: часть СИ, часть ПИ и список ошибок.</returns>
    public static (string SiPart, string PiPart, List<string> Errors) SplitSiFromPiStrict(string input)
    {
      var errors = new List<string>();

      if (string.IsNullOrWhiteSpace(input))
        return ("", "", errors);

      var matchAfterPi = Regex.Match(input, @"\bПИ\b\s*(.*)", RegexOptions.IgnoreCase);
      var matchAfterSi = Regex.Match(input, @"\bСИ\b\s*(.*)", RegexOptions.IgnoreCase);

      string tail;

      if (matchAfterPi.Success)
        tail = matchAfterPi.Groups[1].Value;
      else if (matchAfterSi.Success)
        tail = matchAfterSi.Groups[1].Value;
      else
      {
        errors.Add("E-NOT-PI-SI");
        return ("", "", errors);
      }

      var voltageRegex = new Regex(@"[+-]?\d+\s*В\s*,?", RegexOptions.IgnoreCase);

      var allVoltages = voltageRegex.Matches(tail);
      if (allVoltages.Count == 0)
      {
        errors.Add("E-NO-VOLTAGE");
        return ("", "", errors);
      }

      // 2) Из всех напряжений выбираем только те, перед которыми нет запятой.
      var validSplitPoints = new List<Match>();

      foreach (Match m in allVoltages)
      {
        int idx = m.Index;

        // Если начинается с 0 — не подходит
        if (idx == 0)
          continue;

        // Берём символ перед напряжением
        char before = tail[idx - 1];

        if (before != ',')
          validSplitPoints.Add(m);
      }

      if (validSplitPoints.Count == 0)
      {
        string piOnly = tail.Trim();
        return ("", piOnly, errors);
      }

      var split = validSplitPoints.Last();
      int splitIndex = split.Index;

      var siRaw = tail[..splitIndex];
      var piRaw = tail[splitIndex..];

      var si = NormalizeSi(siRaw).Trim();
      var pi = piRaw.Trim();

      return (si, pi, errors);
    }

    /// <summary>
    /// Нормализует текст части СИ (пробелы и форматирование).
    /// </summary>
    /// <param name="s">Исходная строка.</param>
    /// <returns>Нормализованная строка СИ.</returns>
    private static string NormalizeSi(string s)
    {
      if (string.IsNullOrWhiteSpace(s)) return s?.Trim() ?? string.Empty;
      s = Regex.Replace(s, @"\s+", " ");  // все пробелы/табы -> один пробел
      s = Regex.Replace(s, @"\s+,", ","); // убрать пробел перед запятой
      s = Regex.Replace(s, @",\s*", ", "); // после запятой — один пробел
      return s.Trim();
    }
  }
}
