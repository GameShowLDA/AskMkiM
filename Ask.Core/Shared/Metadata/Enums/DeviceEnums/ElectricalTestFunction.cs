using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ask.Core.Shared.Metadata.Enums.DeviceEnums
{
  /// <summary>
  /// Функция электрического контроля.
  /// Определяет измеряемую величину или выполняемое испытание,
  /// независимо от типа используемого прибора.
  /// </summary>
  public enum ElectricalTestFunction
  {
    /// <summary>
    /// Не определено.
    /// </summary>
    None = 0,

    /// <summary>
    /// Испытание электрической прочности изоляции переменным напряжением (ACW).
    /// </summary>
    DielectricWithstandAC = 1,

    /// <summary>
    /// Испытание электрической прочности изоляции постоянным напряжением (DCW).
    /// </summary>
    DielectricWithstandDC = 2,

    /// <summary>
    /// Измерение сопротивления изоляции (IR).
    /// </summary>
    InsulationResistance = 3,

    /// <summary>
    /// Переменное напряжение.
    /// </summary>
    ACVoltage = 10,

    /// <summary>
    /// Постоянное напряжение.
    /// </summary>
    DCVoltage = 11,

    /// <summary>
    /// Электрическое сопротивление.
    /// </summary>
    Resistance = 12,

    /// <summary>
    /// Электрическая ёмкость.
    /// </summary>
    Capacitance = 13,

    /// <summary>
    /// Прозвонка (проверка целостности цепи).
    /// </summary>
    Continuity = 14
  }
}
