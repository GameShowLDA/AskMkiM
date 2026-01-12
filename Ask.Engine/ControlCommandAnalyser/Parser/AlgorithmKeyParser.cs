using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Parser
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

    public static List<(string Key, bool HasError)> ExtractKeysWithTrailingCommaCheck(string line, BaseCommandModel model)
    {
      var result = new List<(string, bool)>();

      if (string.IsNullOrWhiteSpace(line))
        return result;

      var allowedEnumKeys = KeysHelper
        .GetAllowedKeysForModel(model)
        .Select(k => k.ToString())
        .ToHashSet();

      // Разбиваем строку на токены
      var tokens = line.Split(new[] { ' ', '\t', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries);

      // Собираем только те, что являются ключами
      var matchedKeys = tokens.Where(token => allowedEnumKeys.Contains(token)).ToList();


      var notAllowedKeys = KeysHelper
        .GetNotAllowedKeysForModel(model)
        .Select(k => k.ToString())
        .ToHashSet();
      var foundNotAllowedKeys = tokens.Where(token => notAllowedKeys.Contains(token)).ToList();

      if (matchedKeys.Count == 0 && foundNotAllowedKeys.Count == 0)
        return result;

      // Проверка: была ли запятая после последнего ключа
      if (matchedKeys.Count > 0)
      {
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
      }
      if (foundNotAllowedKeys.Count > 0)
      {
        string lastKey = foundNotAllowedKeys.Last();
        int lastKeyIndex = line.LastIndexOf(lastKey, StringComparison.Ordinal);
        bool hasTrailingComma = false;

        if (lastKeyIndex >= 0 && lastKeyIndex + lastKey.Length < line.Length)
        {
          char nextChar = line[lastKeyIndex + lastKey.Length];
          hasTrailingComma = nextChar == ',';
        }
        for (int i = 0; i < foundNotAllowedKeys.Count; i++)
        {
          var key = foundNotAllowedKeys[i];
          bool hasError = (i == foundNotAllowedKeys.Count - 1) && !hasTrailingComma;
          result.Add((key, true));
        }
      }
      return result;
    }
  }
}
