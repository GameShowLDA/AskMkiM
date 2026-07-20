using System.ComponentModel;

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
    [Description("Не определено")]
    None = 0,

    /// <summary>
    /// Испытание электрической прочности изоляции переменным напряжением (ACW).
    /// </summary>
    [Description("Испытание электрической прочности изоляции переменным напряжением")]
    DielectricWithstandAC = 1,

    /// <summary>
    /// Испытание электрической прочности изоляции постоянным напряжением (DCW).
    /// </summary>
    [Description("Испытание электрической прочности изоляции постоянным напряжением")]
    DielectricWithstandDC = 2,

    /// <summary>
    /// Измерение сопротивления изоляции (IR).
    /// </summary>
    [Description("Измерение сопротивления изоляции")]
    InsulationResistance = 3,

    /// <summary>
    /// Измерение переменного напряжения.
    /// </summary>
    [Description("Измерение переменного напряжения")]
    ACVoltage = 10,

    /// <summary>
    /// Измерение постоянного напряжения.
    /// </summary>
    [Description("Измерение постоянного напряжения")]
    DCVoltage = 11,

    /// <summary>
    /// Измерение электрического сопротивления.
    /// </summary>
    [Description("Измерение электрического сопротивления")]
    Resistance = 12,

    /// <summary>
    /// Измерение электрической ёмкости.
    /// </summary>
    [Description("Измерение электрической ёмкости")]
    Capacitance = 13,

    /// <summary>
    /// Проверка целостности электрической цепи (прозвонка).
    /// </summary>
    [Description("Проверка целостности цепи (прозвонка)")]
    Continuity = 14,

    /// <summary>
    /// Проверка диода.
    /// </summary>
    [Description("Проверка диода")]
    Diode = 15
  }
}