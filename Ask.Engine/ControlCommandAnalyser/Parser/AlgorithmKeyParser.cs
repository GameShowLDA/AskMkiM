using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser.Parser
{
  /// <summary>
  /// Утилита для извлечения и анализа ключей алгоритма из строки команды.
  /// <para>
  /// Предоставляет методы:
  /// <list type="bullet">
  /// <item><description>поиска ключей, определённых в перечислении <see cref="AlgorithmKey"/>;</description></item>
  /// <item><description>разделения строки на токены;</description></item>
  /// <item><description>проверки допустимости ключей для конкретной модели команды;</description></item>
  /// <item><description>валидации формата записи (например, наличия запятой).</description></item>
  /// </list>
  /// </para>
  /// </summary>
  public static class AlgorithmKeyParser
  {
    /// <summary>
    /// Извлекает все ключи алгоритма из строки команды,
    /// которые присутствуют в перечислении <see cref="AlgorithmKey"/>.
    /// </summary>
    /// <param name="line">Исходная строка команды.</param>
    /// <returns>
    /// Список уникальных найденных ключей.
    /// Если строка пустая — возвращается пустой список.
    /// </returns>
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
    /// Извлекает ключи алгоритма и проверяет корректность их использования,
    /// включая принадлежность к разрешённым или запрещённым ключам.
    /// </summary>
    /// <param name="line">Исходная строка команды.</param>
    /// <param name="model">Модель команды, для которой выполняется проверка.</param>
    /// <returns>
    /// Список кортежей, где:
    /// <list type="bullet">
    /// <item><description><c>Key</c> — найденный ключ;</description></item>
    /// <item><description><c>HasError</c> — признак ошибки (например, ключ запрещён).</description></item>
    /// </list>
    /// </returns>
    public static List<(string Key, bool HasError)> ExtractKeysWithTrailingCommaCheck(string line, BaseCommandModel model)
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

    /// <summary>
    /// Разбивает строку на токены по разделителям (пробел, табуляция, запятая, точка с запятой, вертикальная черта).
    /// </summary>
    /// <param name="line">Строка для разбиения.</param>
    /// <returns>Список токенов без пустых значений.</returns>
    private static List<string> SplitTokens(string line) =>
  line.Split(new[] { ' ', '\t', ',', ';', '|' },
             StringSplitOptions.RemoveEmptyEntries)
      .ToList();

    /// <summary>
    /// Получает множество разрешённых ключей алгоритма для указанной модели команды.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <returns>Множество строковых представлений разрешённых ключей.</returns>
    private static HashSet<string> GetAllowedKeys(BaseCommandModel model) =>
  KeysHelper.GetAllowedKeysForModel(model)
            .Select(k => k.ToString())
            .ToHashSet();

    /// <summary>
    /// Получает множество запрещённых ключей алгоритма для указанной модели команды.
    /// </summary>
    /// <param name="model">Модель команды.</param>
    /// <returns>Множество строковых представлений запрещённых ключей.</returns>
    private static HashSet<string> GetNotAllowedKeys(BaseCommandModel model) =>
      KeysHelper.GetNotAllowedKeysForModel(model)
                .Select(k => k.ToString())
                .ToHashSet();

    /// <summary>
    /// Обрабатывает группу ключей и формирует результат с признаком ошибки.
    /// </summary>
    /// <param name="line">Исходная строка команды.</param>
    /// <param name="keys">Список ключей для обработки.</param>
    /// <param name="markAsError">
    /// Признак, указывающий, следует ли пометить ключи как ошибочные
    /// (например, если они запрещены).
    /// </param>
    /// <returns>Список кортежей (ключ, признак ошибки).</returns>
    private static List<(string Key, bool HasError)> ProcessKeyGroup(string line, List<string> keys, bool markAsError)
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
    /// Проверяет, находится ли запятая сразу после указанного ключа в строке.
    /// </summary>
    /// <param name="line">Исходная строка.</param>
    /// <param name="key">Ключ для проверки.</param>
    /// <returns>
    /// true — если после ключа стоит запятая;  
    /// false — если запятая отсутствует или ключ не найден.
    /// </returns>
    private static bool HasTrailingComma(string line, string key)
    {
      int index = line.LastIndexOf(key, StringComparison.Ordinal);
      if (index < 0 || index + key.Length >= line.Length)
        return false;

      return line[index + key.Length] == ',';
    }
  }
}
