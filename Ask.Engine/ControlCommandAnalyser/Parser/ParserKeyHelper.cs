using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Engine.ControlCommandAnalyser.Attributes;

namespace Ask.Engine.ControlCommandAnalyser.Parser
{
  public static class ParserKeyHelper
  {
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
