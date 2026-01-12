using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Mode
{
  /// <summary>
  /// Интерфейс для режима пробоя (DCW Mode Breakdown).
  /// Определяет функциональность, связанную с тестированием пробоя постоянным напряжением.
  /// </summary>
  public interface IDcwModeBreakdown
  {

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

    IConfigurationProvider<DcwConfiguration> Config { get; set; }

    IMeasurable Measure { get; set; }
    BreakdownTypeMode ModeType => BreakdownTypeMode.DCW;
  }
}
