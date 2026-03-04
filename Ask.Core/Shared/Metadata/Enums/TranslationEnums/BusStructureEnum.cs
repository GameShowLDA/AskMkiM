using System.ComponentModel;

namespace Ask.Core.Shared.Metadata.Enums.TranslationEnums
{
  public class BusStructureEnum
  {
    public enum Type
    {
      /// <summary>
      /// Структура не указана - значение по умолчанию.
      /// </summary>
      [Description("0")]
      None,

      /// <summary>
      /// Двухшинная структура стойки коммутации.
      /// </summary>
      [Description("2")]
      Bus2,

      /// <summary>
      /// Четырехшинная структура стойки коммутации.
      /// </summary>
      [Description("4")]
      Bus4,

      /// <summary>
      /// Шестишинная структура стойки коммутации.
      /// </summary>
      [Description("6")]
      Bus6,

      /// <summary>
      /// Восьмишинная структура стойки коммутации.
      /// </summary>
      [Description("8")]
      Bus8,

      /// <summary>
      /// Комбинированное подключение к шинам.
      /// </summary>
      [Description("К")]
      BusCombined,
    }

  }
}
