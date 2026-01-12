using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Attributes;
using Ask.Engine.ControlCommandAnalyser.Model;

namespace Ask.Engine.ControlCommandAnalyser
{
  public static class KeysHelper
  {
    private static readonly Dictionary<Type, AlgorithmKey[]> Cache = new();

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
    public static AlgorithmKey[] GetNotAllowedKeysForModel(BaseCommandModel model)
    {
      // 1. Получаем разрешённые ключи через уже существующий кэш
      Type type = model.GetType();

      if (!Cache.TryGetValue(type, out var allowedKeys))
      {
        var attr = type.GetCustomAttributes(typeof(AllowedKeysAttribute), true)
                       .Cast<AllowedKeysAttribute>()
                       .FirstOrDefault();

        allowedKeys = attr?.Keys ?? Array.Empty<AlgorithmKey>();
        Cache[type] = allowedKeys;
      }

      // 2. Получаем полный список всех ключей enum
      var allKeys = (AlgorithmKey[])Enum.GetValues(typeof(AlgorithmKey));

      // 3. Вычисляем запрещённые
      return allKeys
          .Where(k => !allowedKeys.Contains(k))
          .ToArray();
    }
  }
}
