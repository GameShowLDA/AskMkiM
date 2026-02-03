using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Metadata.Enums.DeviceEnums
{
  /// <summary>
  /// Определяет режим работы мультиметра.
  /// </summary>
  public enum MultimeterTypeMode
  {
    /// <summary>
    /// Режим не задан.
    /// </summary>
    None = 0,

    /// <summary>
    /// Измерение переменного напряжения (AC).
    /// </summary>
    AcVoltage = 1,

    /// <summary>
    /// Измерение постоянного напряжения (DC).
    /// </summary>
    DcVoltage = 2,

    /// <summary>
    /// Измерение ёмкости.
    /// </summary>
    Capacitance = 3,

    /// <summary>
    /// Проверка целостности цепи (прозвонка).
    /// </summary>
    Continuity = 4,

    /// <summary>
    /// Измерение электрического сопротивления.
    /// </summary>
    Resistance = 5,
  }

}
