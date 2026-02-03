using System.ComponentModel;

namespace Ask.Core.Shared.Metadata.Enums.TranslationEnums
{
  public class ElementEnabling
  {
    public enum Type
    {
      /// <summary>
      /// Прямое подключение элемента.
      /// </summary>
      [Description("+")]
      Direct,

      /// <summary>
      /// Обратное подключение элемента.
      /// </summary>
      [Description("-")]
      Reverse,
    }
  }
}
