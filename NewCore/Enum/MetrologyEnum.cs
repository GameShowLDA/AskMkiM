using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewCore.Enum
{
  /// <summary>
  /// Перечесления для метрологии.
  /// </summary>
  public class MetrologyEnum
  {
    /// <summary>
    /// Определяет логические роли устройств, используемых в метрологических режимах.
    /// </summary>
    public enum MetrologicalModeRole
    {
      /// <summary>
      /// Коммутация сопротивления (например, КС).
      /// </summary>
      KC,

      /// <summary>
      /// Измеритель сопротивления изоляции (например, ППУ).
      /// </summary>
      IE,

      /// <summary>
      /// Прецизионный мультиметр (для измерений сопротивления, ёмкости и т.д.).
      /// </summary>
      PR,

      /// <summary>
      /// Источник напряжения или тока.
      /// </summary>
      CI,
    }
  }
}
