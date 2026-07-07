using System.ComponentModel;

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
    [Description("Режим не задан")]
    None = 0,

    /// <summary>
    /// Измерение переменного напряжения (AC).
    /// </summary>
    [Description("Измерение переменного напряжения")]
    AcVoltage = 1,

    /// <summary>
    /// Измерение постоянного напряжения (DC).
    /// </summary>
    [Description("Измерение постоянного напряжения")]
    DcVoltage = 2,

    /// <summary>
    /// Измерение ёмкости.
    /// </summary>
    [Description("Измерение ёмкости")]
    Capacitance = 3,

    /// <summary>
    /// Проверка целостности электрической цепи (прозвонка).
    /// </summary>
    [Description("Проверка целостности цепи (прозвонка)")]
    Continuity = 4,

    /// <summary>
    /// Измерение электрического сопротивления.
    /// </summary>
    [Description("Измерение электрического сопротивления")]
    Resistance = 5,

    /// <summary>
    /// Проверка диода.
    /// </summary>
    [Description("Проверка диода")]
    Diode = 6,
  }
}
