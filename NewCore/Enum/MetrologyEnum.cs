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
      /// Контроль сопротивления (Мультиметр).
      /// </summary>
      KC,

      /// <summary>
      /// Измерение ёмкости (Мультиметр).
      /// </summary>
      IE,

      /// <summary>
      /// Проверка релейная (Мультиметр).
      /// </summary>
      PR,

      /// <summary>
      /// Сопротивление изоляции (ППУ).
      /// </summary>
      CI,

      /// <summary>
      /// Прочность изоляции (ППУ).
      /// </summary>
      PI,

      /// <summary>
      /// Контроль напряжения (КН).
      /// </summary>
      KN,
    }
  }
}
