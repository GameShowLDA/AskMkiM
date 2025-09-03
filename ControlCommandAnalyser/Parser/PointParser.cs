using ControlCommandAnalyser.Model;
using ControlCommandAnalyser.Model.Chains;
using System.Text.RegularExpressions;
using Utilities.Models;

namespace ControlCommandAnalyser.Parser
{
  public class PointParser
  {
    /// <summary>
    /// Разбор блока точек '*...*' в SchemeModel.
    /// Правила:
    /// - Одна '*' внутри блока разделяет ЦЕПИ.
    /// - '#': части внутри одной цепи.
    /// - ',': перечисление токенов (точка или диапазон).
    /// - Диапазоны: 87-90, 1.2.7-10, 1.2.7-1.2.10, Х51/51-60.
    /// - Спец-кейс: токен, заканчивающийся '-' и следующий сегмент после '*' — конец диапазона
    ///   (пример 'Х51/51-*60') → каждая раскрытая точка = отдельная цепь.
    /// - Для КС: одиночная точка (один исходный токен БЕЗ '-') в части запрещена.
    /// </summary>
    public static (SchemeModel, List<ErrorItem>) ParsePoints(string expr, string mnemonic, RmCommandModel rmCommandModel)
    {
      var errors = new List<ErrorItem>();
      var chainModels = new List<GroupModel>();

      // Убираем все пробелы/табы/переводы строк и внешние '*'
      expr = Regex.Replace(expr ?? string.Empty, @"\s+", "");
      expr = expr.Trim('*');
      if (string.IsNullOrEmpty(expr))
        return (null, errors);

      // Цепи теперь разделяются одной '*'
      var chainSegs = expr.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries);

      for (int i = 0; i < chainSegs.Length; i++)
      {
        string chainSegOriginal = chainSegs[i];
        if (string.IsNullOrWhiteSpace(chainSegOriginal))
          continue;

        // Части внутри цепи по '#'
        var partTokens = chainSegOriginal.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(CleanToken)
                                         .Where(t => !string.IsNullOrEmpty(t))
                                         .ToList();

        // === Спец-кейс: одна часть, один токен, который заканчивается '-' и конец диапазона в СЛЕДУЮЩЕМ сегменте цепи ===
        if (partTokens.Count == 1)
        {
          var rawTokensSingle = partTokens[0].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(CleanToken)
                                             .Where(t => !string.IsNullOrEmpty(t))
                                             .ToList();

          if (rawTokensSingle.Count == 1 && rawTokensSingle[0].EndsWith("-") && (i + 1) < chainSegs.Length)
          {
            string leftWithDash = rawTokensSingle[0];                  // напр. "Х51/51-"
            string rightSeg = CleanToken(chainSegs[i + 1]);            // напр. "60"
            // правый сегмент не должен содержать '#', ',' — иначе это не продолжение диапазона
            if (!string.IsNullOrEmpty(rightSeg)
                && !rightSeg.Contains('#')
                && !rightSeg.Contains(','))
            {
              string combinedRange = leftWithDash + rightSeg;          // "Х51/51-60"
              var expanded = ExpandRangeToken(combinedRange, errors);
              if (expanded.Count > 0)
              {
                // Каждая точка -> отдельная цепь
                foreach (var one in expanded)
                {
                  var (ok1, pts1) = CommandPostAnalyzer.GetPointsModel(new List<string> { one }, rmCommandModel.PointsMap);
                  var part = new ChainModel(new List<PointModel>(pts1 ?? new List<PointModel>()));
                  chainModels.Add(new GroupModel(new List<ChainModel> { part }));
                }
                i++; // съедаем следующий сегмент, т.к. он использован как конец диапазона
                continue; // переходим к следующей исходной "цепи"
              }
            }
          }
        }
        // === конец спец-кейса ===

        // Обычная обработка: каждая часть -> PartChainModel (после раскрытия диапазонов)
        var currentChainParts = new List<ChainModel>();

        foreach (var part in partTokens)
        {
          // Токены в части по запятым
          var rawTokens = part.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(CleanToken)
                              .Where(t => !string.IsNullOrEmpty(t))
                              .ToList();

          // Раскрываем диапазоны
          var expandedTokens = new List<string>();
          for (int k = 0; k < rawTokens.Count; k++)
          {
            var tok = rawTokens[k];
            if (tok.Contains('-'))
              expandedTokens.AddRange(ExpandRangeToken(tok, errors));
            else
              expandedTokens.Add(tok);
          }

          // Проверка "одиночной точки" для КС: только если исходно был один токен без '-'
          bool isSingleOriginalToken = rawTokens.Count == 1 && !rawTokens[0].Contains('-');
          if (string.Equals(mnemonic, "КС", StringComparison.OrdinalIgnoreCase)
              && isSingleOriginalToken
              && expandedTokens.Count == 1)
          {
            errors.Add(new ErrorItem
            {
              Description = $"Нельзя указывать одиночную точку (часть цепи содержит только: {expandedTokens[0]}).",
              Code = Utilities.Errors.ErrorCode.Gen_InvalidOnePointUse
            });
          }

          // Преобразуем строки-точки в PointModel через карту РМ
          var (ok2, pts2) = CommandPostAnalyzer.GetPointsModel(expandedTokens, rmCommandModel.PointsMap);
          currentChainParts.Add(new ChainModel(new List<PointModel>(pts2 ?? new List<PointModel>())));
        }
        if (currentChainParts.Count > 0)
        {
          for (int cp = 0; cp < currentChainParts.Count; cp++)
          {
            var chain = currentChainParts[cp];
            for (int pm = 0; pm < chain.PointModels.Count; pm++)
            {
              if (pm == 0 && cp == 0)
              {
                chain.PointModels[pm].PointType = PointType.Type.Star;
              }
              else if (pm == 0 && cp > 0)
              {
                chain.PointModels[pm].PointType = PointType.Type.Hash;
              }
              else
              {
                chain.PointModels[pm].PointType = PointType.Type.Comma;
              }
              chain.PointModels[pm].Mnemonic = mnemonic;
            }
          }

        }
          chainModels.Add(new GroupModel(new List<ChainModel>(currentChainParts)));
      }

      return (new SchemeModel(chainModels), errors);
    }

    /// <summary>
    /// Раскрывает диапазон: "87-90", "1.2.7-10", "1.2.7-1.2.10", "Х51/51-60".
    /// </summary>
    private static List<string> ExpandRangeToken(string token, List<ErrorItem> errors)
    {
      var result = new List<string>();

      token = CleanToken(token);  // убрать возможные '*'

      int dashIndex = token.IndexOf('-');
      if (dashIndex < 0)
        return result;

      string left = CleanToken(token.Substring(0, dashIndex));
      string right = CleanToken(token.Substring(dashIndex + 1));

      if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
      {
        errors.Add(new ErrorItem
        {
          Description = $"Неверный диапазон точек: {token}.",
          Code = Utilities.Errors.ErrorCode.Gen_InvalidRange
        });
        return result;
      }

      // Выделяем префикс + старт (префикс заканчивается на '.' или '/')
      if (!TrySplitPrefixAndNumber(left, out string leftPrefix, out int startNum))
      {
        errors.Add(new ErrorItem
        {
          Description = $"Неверное начало диапазона: {left} (в {token}).",
          Code = Utilities.Errors.ErrorCode.Gen_InvalidRange
        });
        return result;
      }

      // Определяем конец диапазона
      int endNum;
      if (TrySplitPrefixAndNumber(right, out string rightPrefix, out int rightNum))
      {
        if (!string.IsNullOrEmpty(rightPrefix) && rightPrefix != leftPrefix)
        {
          errors.Add(new ErrorItem
          {
            Description = $"Несовместимые префиксы в диапазоне: {token} (\"{leftPrefix}\" vs \"{rightPrefix}\").",
            Code = Utilities.Errors.ErrorCode.Gen_InvalidRange
          });
          return result;
        }
        endNum = rightNum;
      }
      else
      {
        if (!int.TryParse(right, out endNum))
        {
          errors.Add(new ErrorItem
          {
            Description = $"Неверный конец диапазона: {right} (в {token}).",
            Code = Utilities.Errors.ErrorCode.Gen_InvalidRange
          });
          return result;
        }
      }

      if (endNum < startNum)
      {
        errors.Add(new ErrorItem
        {
          Description = $"Неверный диапазон точек (конец меньше начала): {token}.",
          Code = Utilities.Errors.ErrorCode.Gen_InvalidRange
        });
        return result;
      }

      for (int n = startNum; n <= endNum; n++)
        result.Add($"{leftPrefix}{n}");

      return result;
    }

    /// <summary>
    /// "префикс" + число. Префикс = всё до последнего '.' или '/' включительно.
    /// </summary>
    private static bool TrySplitPrefixAndNumber(string token, out string prefix, out int number)
    {
      prefix = "";
      number = 0;

      int dot = token.LastIndexOf('.');
      int slash = token.LastIndexOf('/');

      int sep = Math.Max(dot, slash);

      if (sep >= 0)
      {
        prefix = token.Substring(0, sep + 1);
        var tail = token.Substring(sep + 1);
        return int.TryParse(tail, out number);
      }
      else
      {
        return int.TryParse(token, out number);
      }
    }

    private static string CleanToken(string t)
    {
      return string.IsNullOrEmpty(t) ? string.Empty : t.Replace("*", "").Trim();
    }
  }
}
