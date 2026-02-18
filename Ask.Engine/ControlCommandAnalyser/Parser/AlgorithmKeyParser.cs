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

      return line
        .Split(new[] { '\t', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
        .Where(token => enumKeys.Contains(token))
        .Distinct()
        .ToList();
    }

    /// <summary>
    /// Извлекает ключи из строки и проверяет наличие запятой после последнего.
    /// </summary>
    public static List<(string Key, bool HasError)>
    ExtractKeysWithTrailingCommaCheck(string line, BaseCommandModel model)
    {
      var result = new List<(string, bool)>();

      if (string.IsNullOrWhiteSpace(line))
        return result;

      var tokens = SplitTokens(line);

      var allowedKeys = GetAllowedKeys(model);
      var notAllowedKeys = GetNotAllowedKeys(model);

      var matchedAllowed = tokens.Where(allowedKeys.Contains).ToList();
      var matchedNotAllowed = tokens.Where(notAllowedKeys.Contains).ToList();

      if (matchedAllowed.Count == 0 && matchedNotAllowed.Count == 0)
        return result;

      result.AddRange(ProcessKeyGroup(line, matchedAllowed, false));
      result.AddRange(ProcessKeyGroup(line, matchedNotAllowed, true));

      return result;
    }

    private static List<string> SplitTokens(string line) =>
  line.Split(new[] { ' ', '\t', ',', ';', '|' },
             StringSplitOptions.RemoveEmptyEntries)
      .ToList();

    private static HashSet<string> GetAllowedKeys(BaseCommandModel model) =>
  KeysHelper.GetAllowedKeysForModel(model)
            .Select(k => k.ToString())
            .ToHashSet();

    private static HashSet<string> GetNotAllowedKeys(BaseCommandModel model) =>
      KeysHelper.GetNotAllowedKeysForModel(model)
                .Select(k => k.ToString())
                .ToHashSet();

    /// <summary>
    /// Проверяет последнюю запятую и формирует результат.
    /// </summary>
    private static List<(string Key, bool HasError)>
    ProcessKeyGroup(string line, List<string> keys, bool markAsError)
    {
      var result = new List<(string, bool)>();

      if (keys.Count == 0)
        return result;

      bool hasTrailingComma = HasTrailingComma(line, keys.Last());

      for (int i = 0; i < keys.Count; i++)
      {
        //bool isLast = i == keys.Count - 1;
        //bool missingComma = isLast && !hasTrailingComma;

        bool error = markAsError;
          //|| missingComma;

        result.Add((keys[i], error));
      }

      return result;
    }

    /// <summary>
    /// Проверяет, стоит ли запятая сразу после ключа.
    /// </summary>
    private static bool HasTrailingComma(string line, string key)
    {
      int index = line.LastIndexOf(key, StringComparison.Ordinal);
      if (index < 0 || index + key.Length >= line.Length)
        return false;

      return line[index + key.Length] == ',';
    }
  }
}
