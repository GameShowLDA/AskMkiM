using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Metadata.Enums.TranslationEnums
{
  public class BusStructureEnum
  {
    public enum Type
    {
      /// <summary>
      /// Двухшинная структура стойки коммутации.
      /// </summary>
      Bus2,

      /// <summary>
      /// Четырехшинная структура стойки коммутации.
      /// </summary>
      Bus4,

      /// <summary>
      /// Шестишинная структура стойки коммутации.
      /// </summary>
      Bus6,

      /// <summary>
      /// Восьмишинная структура стойки коммутации.
      /// </summary>
      Bus8,

      /// <summary>
      /// Комбинированное подключение к шинам.
      /// </summary>
      BusCombined,
    }
  }
}
