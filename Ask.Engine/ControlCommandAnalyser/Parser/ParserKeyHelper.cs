using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Attributes;

namespace Ask.Engine.ControlCommandAnalyser.Parser
{
  /// <summary>
  /// Вспомогательный класс для работы с ключами алгоритма парсеров.
  /// Предоставляет методы получения допустимых ключей,
  /// определённых через атрибут <see cref="AllowedKeysAttribute"/>.
  /// </summary>
  public static class ParserKeyHelper
  {
    /// <summary>
    /// Возвращает набор допустимых ключей алгоритма,
    /// объявленных для указанного парсера.
    /// </summary>
    /// <param name="parser">Экземпляр парсера команды.</param>
    /// <returns>
    /// Множество допустимых ключей <see cref="AlgorithmKey"/>.
    /// Если атрибут отсутствует — возвращается пустой набор.
    /// </returns>
    /// <remarks>
    /// Метод использует рефлексию для чтения атрибута
    /// <see cref="AllowedKeysAttribute"/> у типа парсера.
    /// </remarks>
    public static HashSet<AlgorithmKey> GetAllowedKeys(ICommandParser parser)
    {
      var attr = parser.GetType()
                       .GetCustomAttributes(typeof(AllowedKeysAttribute), false)
                       .FirstOrDefault() as AllowedKeysAttribute;

      return attr != null
          ? new HashSet<AlgorithmKey>(attr.Keys)
          : new HashSet<AlgorithmKey>();
    }
  }
}
