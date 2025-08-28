using ControlCommandAnalyser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ControlCommandAnalyser.Parser
{
  /// <summary>
  /// Утилита для извлечения ключей алгоритма из строки команды.
  /// </summary>
  public static class AlgorithmKeyParser
  {
    /// <summary>
    /// Извлекает все ключи из текста команды, присутствующие в перечислении AlgorithmKey.
    /// </summary>
    /// <param name="line">Исходная строка команды.</param>
    /// <returns>Список найденных ключей.</returns>
    public static List<string> ExtractKeys(string line)
    {
      if (string.IsNullOrWhiteSpace(line))
        return new();

      var enumKeys = Enum.GetNames(typeof(AlgorithmKey));

      // Разбиваем строку и ищем совпадения с допустимыми ключами (точно по имени)
      return line
        .Split(new[] { '\t', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
        .Where(token => enumKeys.Contains(token))
        .Distinct()
        .ToList();
    }

    public static List<(string Key, bool HasError)> ExtractKeysWithTrailingCommaCheck(string line)
    {
      var result = new List<(string, bool)>();

      if (string.IsNullOrWhiteSpace(line))
        return result;

      var enumKeys = Enum.GetNames(typeof(AlgorithmKey));

      // Разбиваем строку на токены
      var tokens = line.Split(new[] { ' ', '\t', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

      // Собираем только те, что являются ключами
      var matchedKeys = tokens.Where(token => enumKeys.Contains(token)).ToList();

      if (matchedKeys.Count == 0)
        return result;

      // Проверка: была ли запятая после последнего ключа
      string lastKey = matchedKeys.Last();
      int lastKeyIndex = line.LastIndexOf(lastKey, StringComparison.Ordinal);

      bool hasTrailingComma = false;

      if (lastKeyIndex >= 0 && lastKeyIndex + lastKey.Length < line.Length)
      {
        char nextChar = line[lastKeyIndex + lastKey.Length];
        hasTrailingComma = nextChar == ',';
      }

      for (int i = 0; i < matchedKeys.Count; i++)
      {
        var key = matchedKeys[i];
        bool hasError = (i == matchedKeys.Count - 1) && !hasTrailingComma;
        result.Add((key, false));
      }

      return result;
    }
  }
}
