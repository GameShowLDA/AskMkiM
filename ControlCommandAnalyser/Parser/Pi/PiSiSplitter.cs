using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ControlCommandAnalyser.Parser.Pi
{
  public static partial class PiSiSplitter
  {
    private static readonly HashSet<string> SiKeys = new(StringComparer.OrdinalIgnoreCase)
    { AlgorithmKey.К.ToString(),
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

    // ------------------------ VALIDATION ------------------------

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

    private static bool IsSiKey(string k) => SiKeys.Contains(k);

    // ------------------------ LEXER ------------------------

    private static List<Tok> Lex(string s)
    {
      var toks = new List<Tok>();
      int i = 0;
      while (i < s.Length)
      {
        // whitespace
        if (char.IsWhiteSpace(s[i]))
        {
          int j = i + 1;
          while (j < s.Length && char.IsWhiteSpace(s[j])) j++;
          toks.Add(new Tok(TokType.Ws, s[i..j], i, j - i));
          i = j;
          continue;
        }

        // comma
        if (s[i] == ',')
        {
          toks.Add(new Tok(TokType.Comma, ",", i, 1));
          i++;
          continue;
        }

        // points block: from '*' up to next whitespace or end-of-line if нет завершающей '*'
        if (s[i] == '*')
        {
          int j = i + 1;
          while (j < s.Length && s[j] != '\n' && !char.IsWhiteSpace(s[j])) j++;
          toks.Add(new Tok(TokType.Points, s[i..j], i, j - i));
          i = j;
          continue;
        }

        // voltage: [+/-]?\d+\s*В
        {
          var m = Regex.Match(s.Substring(i), @"^[\+\-]?\d+\s*В", RegexOptions.IgnoreCase);
          if (m.Success)
          {
            toks.Add(new Tok(TokType.Volt, m.Value.Trim(), i, m.Length));
            i += m.Length;
            continue;
          }
        }

        // time: \d+\s*С
        {
          var m = Regex.Match(s.Substring(i), @"^\d+\s*С", RegexOptions.IgnoreCase);
          if (m.Success)
          {
            toks.Add(new Tok(TokType.Time, m.Value.Trim(), i, m.Length));
            i += m.Length;
            continue;
          }
        }

        // resistance: \d+<МОм / кОм / ГОм (МОМ нормализуем в МОм)
        {
          var m = Regex.Match(s.Substring(i), @"^\d+\s*<\s*(МОМ|МОм|кОм|ГОм)", RegexOptions.IgnoreCase);
          if (m.Success)
          {
            toks.Add(new Tok(TokType.Res, NormalizeRes(m.Value), i, m.Length));
            i += m.Length;
            continue;
          }
        }

        // keys: Т1 / T1, или одиночный П С К Д И Г — перед запятой/пробелом/табом/концом
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

        // fallback: копим до ближайшего разделителя
        int k = i + 1;
        while (k < s.Length && !char.IsWhiteSpace(s[k]) && s[k] != ',' && s[k] != '*') k++;
        toks.Add(new Tok(TokType.Other, s[i..k], i, k - i));
        i = k;
      }

      // отфильтруем чистые разделители
      return toks.Where(t => t.Type is not TokType.Ws and not TokType.Comma).ToList();
    }

    private static string NormalizeRes(string text)
    {
      var t = Regex.Replace(text, @"\s+", "");
      t = Regex.Replace(t, "МОМ", "МОм", RegexOptions.IgnoreCase);
      return t;
    }

    // ------------------------ HELPERS ------------------------

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

    private static string Reconstruct(string input, IEnumerable<Tok> tokens)
    {
      var list = tokens.ToList();
      if (list.Count == 0) return "";
      int from = list.First().Start;
      int to = list.Last().End;
      return input.Substring(from, to - from);
    }

    // Вставь внутрь PiSiSplitter
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


    //private static string NormalizeSi(string s)
    //{
    //  if (string.IsNullOrWhiteSpace(s)) return s?.Trim() ?? string.Empty;
    //  s = Regex.Replace(s, @"\s+", " ");
    //  s = Regex.Replace(s, @"\s+,", ",");
    //  s = Regex.Replace(s, @",\s*", ", ");
    //  return s.Trim();
    //}
  }

  public static partial class PiSiSplitter
  {
    // Включить/выключить строгую трактовку
    public static bool StrictWhitespaceMode { get; set; } = true;

    // Жёсткая граница между СИ и ПИ: 2+ пробела или любой \t
    private static readonly Regex StrictBoundaryRegex =
      new(@"(?<=\S)(?:\t| {2,})(?=\S)", RegexOptions.Compiled);

    // Внутри частей запрещаем табы и 2+ пробелов
    private static readonly Regex BadInnerWhitespaceRegex =
      new(@"(?:\t| {2,})", RegexOptions.Compiled);

    /// <summary>
    /// Разделение СИ/ПИ в "строгом" режиме:
    /// - граница = 2+ пробелов или таб;
    /// - внутри частей допускается максимум один пробел (запятая+пробел), табы запрещены;
    /// - если строгая граница не найдена — fallback к обычной грамматике SplitSiFromPi.
    /// </summary>
    public static (string SiPart, string PiPart, List<string> Errors) SplitSiFromPiStrict(string input)
    {
      var errors = new List<string>();

      if (string.IsNullOrWhiteSpace(input))
        return ("", "", errors);

      if (StrictWhitespaceMode)
      {
        var m = StrictBoundaryRegex.Matches(input);

        if (m.Count >= 1)
        {
          // Берём ПЕРВУЮ «жёсткую» границу как разделитель СИ/ПИ.
          // (Опционально: если m.Count > 1 — добавим предупреждение.)
          if (m.Count > 1)
            errors.Add("E-WS-AMB: найдено несколько жёстких разделителей, используется первый.");

          int cutStart = m[0].Index;
          int cutLen = m[0].Length;

          var siRaw = input[..cutStart];
          var piRaw = input[(cutStart + cutLen)..];

          // Проверка «внутренней» чистоты пробелов
          if (BadInnerWhitespaceRegex.IsMatch(siRaw))
            errors.Add("E-WS-SI: внутри параметров СИ есть таб/двойные пробелы (недопустимо в strict).");

          if (BadInnerWhitespaceRegex.IsMatch(piRaw))
            errors.Add("E-WS-PI: внутри параметров ПИ есть таб/двойные пробелы (недопустимо в strict).");

          // Нормализуем вид СИ (запятые, один пробел)
          var si = NormalizeSi(siRaw);
          var pi = piRaw.Trim(); // ПИ вид оставляем как в исходнике

          return (si, pi, errors);
        }
      }

      // Fallback к «умной» грамматике, если строгая граница не найдена
      var (siPart, piPart) = SplitSiFromPi(input);
      return (siPart, piPart, errors);
    }

    /// <summary>
    /// Удобный адаптер: сначала пробуем strict, при неуспехе — обычный.
    /// Подставь этот вызов вместо прямого SplitSiFromPi, если переходишь на строгие правила.
    /// </summary>
    public static (string SiPart, string PiPart) SplitPreferStrict(string input)
    {
      var (si, pi, _) = SplitSiFromPiStrict(input);
      return (si, pi);
    }

    // Нормализация СИ (табы/множественные пробелы -> один пробел; " , " -> ", ")
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
