using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class PointParser
{
  /// <summary>
  /// Парсит строку и возвращает список всех точек по очереди.
  /// </summary>
  public static List<string> ParsePoints(string expr)
  {
    var points = new List<string>();

    // Удаляем пробелы и ведущие/замыкающие *
    expr = expr.Replace(" ", "").Trim('*');
    if (string.IsNullOrEmpty(expr)) return points;

    // Разбиваем по *
    var tokens = expr.Split(new[] { '*' }, StringSplitOptions.RemoveEmptyEntries);

    // Перебираем токены
    for (int i = 0; i < tokens.Length; i++)
    {
      string token = tokens[i];

      // Если диапазон: ищем паттерн вида PREFIX/START- (например, X17/1-)
      var rangeMatch = Regex.Match(token, @"^(?<prefix>.+?/)(?<start>\d+)-$");
      if (rangeMatch.Success && i + 1 < tokens.Length)
      {
        // Следующий токен - конец диапазона (например, 19)
        var endToken = tokens[i + 1];
        if (Regex.IsMatch(endToken, @"^\d+$"))
        {
          string prefix = rangeMatch.Groups["prefix"].Value;
          int start = int.Parse(rangeMatch.Groups["start"].Value);
          int end = int.Parse(endToken);

          // Добавляем все точки диапазона
          for (int n = start; n <= end; n++)
          {
            points.Add($"{prefix}{n}");
          }
          i++; // Пропускаем следующий токен, он уже обработан
          continue;
        }
      }

      // Просто точка (например, K/1)
      points.Add(token);
    }

    return points;
  }
}