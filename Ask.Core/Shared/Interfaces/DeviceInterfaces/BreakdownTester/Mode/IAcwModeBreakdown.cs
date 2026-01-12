using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Mode
{
  /// <summary>
  /// Управление режимом ACW на пробойной установке.
  /// </summary>
  public interface IAcwModeBreakdown
  {

    /// <summary>
    /// Тип режима работы устройства (например, ACW, DCW, IR и т.д.).
    /// </summary>
    BreakdownTypeMode ModeType => BreakdownTypeMode.ACW;

    /// <summary>
    /// Управление режимом работы устройства (установка и проверка текущего режима).
    /// </summary>
    IModeConfigurable Mode { get; set; }

    /// <summary>
    /// Управление напряжением (установка и считывание параметра Voltage).
    /// </summary>
    IVoltageConfigurable Voltage { get; set; }

    /// <summary>
    /// Управление пределами тока (верхний и нижний токовые лимиты).
    /// </summary>
    ICurrentLimitsConfigurable CurrentLimits { get; set; }

    /// <summary>
    /// Управление временными параметрами (время теста и время нарастания напряжения).
    /// </summary>
    ITimeConfigurable Time { get; set; }

    /// <summary>
    /// Управление параметром смещения (Offset).
    /// </summary>
    IOffsetConfigurable Offset { get; set; }

    /// <summary>
    /// Управление параметром тока дуги (Arc Current).
    /// </summary>
    IArcCurrentConfigurable ArcCurrent { get; set; }

    IFrequencyConfigurable FrequencyConfigurable { get; set; }

    IMeasurable Measure { get; set; }

    IConfigurationProvider<AcwConfiguration> Config { get; set; }
  }
}
