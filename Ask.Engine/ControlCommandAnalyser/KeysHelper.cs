using Ask.Core.Shared.DTO.Executor;
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
