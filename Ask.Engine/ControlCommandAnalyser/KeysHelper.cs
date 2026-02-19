using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser
{
  /// <summary>
  /// Вспомогательный класс для получения допустимых и недопустимых ключей алгоритма
  /// для моделей команд. Использует кэширование для ускорения повторных вызовов.
  /// </summary>
  /// <remarks>
  /// Допустимые ключи извлекаются из атрибута <see cref="AllowedKeysAttribute"/>,
  /// объявленного у типа модели команды.
  /// </remarks>
  public static class KeysHelper
  {
    private static readonly Dictionary<Type, AlgorithmKey[]> Cache = new();

    /// <summary>
    /// Возвращает список допустимых ключей алгоритма для указанной модели команды.
    /// </summary>
    /// <param name="model">Экземпляр модели команды.</param>
    /// <returns>
    /// Массив допустимых значений <see cref="AlgorithmKey"/>.
    /// Если атрибут отсутствует — возвращается пустой массив.
    /// </returns>
    /// <remarks>
    /// Результат кэшируется по типу модели для повышения производительности.
    /// </remarks>
    public static AlgorithmKey[] GetAllowedKeysForModel(BaseCommandModel model)
    {
      Type type = model.GetType();

      if (Cache.TryGetValue(type, out var keys))
        return keys;

      var attr = type.GetCustomAttributes(typeof(AllowedKeysAttribute), true)
                     .Cast<AllowedKeysAttribute>()
                     .FirstOrDefault();

      keys = attr?.Keys ?? Array.Empty<AlgorithmKey>();
      Cache[type] = keys;

      return keys;
    }

    /// <summary>
    /// Возвращает список ключей алгоритма, которые не разрешены
    /// для указанной модели команды.
    /// </summary>
    /// <param name="model">Экземпляр модели команды.</param>
    /// <returns>
    /// Массив значений <see cref="AlgorithmKey"/>, отсутствующих
    /// в списке допустимых ключей.
    /// </returns>
    /// <remarks>
    /// Метод использует кэш допустимых ключей и формирует список
    /// недопустимых путём исключения их из полного перечисления.
    /// </remarks>
    public static AlgorithmKey[] GetNotAllowedKeysForModel(BaseCommandModel model)
    {
      Type type = model.GetType();

      if (!Cache.TryGetValue(type, out var allowedKeys))
      {
        var attr = type.GetCustomAttributes(typeof(AllowedKeysAttribute), true)
                       .Cast<AllowedKeysAttribute>()
                       .FirstOrDefault();

        allowedKeys = attr?.Keys ?? Array.Empty<AlgorithmKey>();
        Cache[type] = allowedKeys;
      }

      var allKeys = (AlgorithmKey[])Enum.GetValues(typeof(AlgorithmKey));

      return allKeys
          .Where(k => !allowedKeys.Contains(k))
          .ToArray();
    }
  }
}
