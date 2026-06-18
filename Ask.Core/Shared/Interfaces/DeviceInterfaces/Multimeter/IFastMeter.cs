using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter
{
  /// <summary>
  /// Интерфейс для быстрого измерителя.
  /// </summary>
  public interface IFastMeter : IAttachableDevice, IDeviceToDtoConverter<FastMeterDto>
  {
    MultimeterTypeMode TypeMode { get; set; }

    /// <summary>
    /// Управление измерением переменного напряжения.
    /// </summary>
    IAcVoltageMeasurement AcVoltageManager { get; set; }

    /// <summary>
    /// Управление измерением ёмкости.
    /// </summary>
    ICapacitanceMeasurement CapacitanceManager { get; set; }

    /// <summary>
    /// Управление измерением проводимости (прозвонка).
    /// </summary>
    IContinuityMeasurement ContinuityManager { get; set; }

    /// <summary>
    /// Управление измерением постоянного напряжения.
    /// </summary>
    IDcVoltageMeasurement DcVoltageManager { get; set; }

    /// <summary>
    /// Управление проверкой диода.
    /// </summary>
    IDiodeMeasurement DiodeManager { get; set; }

    /// <summary>
    /// Управление измерением сопротивления.
    /// </summary>
    IResistanceMeasurement ResistanceManager { get; set; }

    /// <summary>
    /// Управление сообщениями.
    /// </summary>
    ITextMessage TextMessage { get; set; }

    /// <summary>
    /// Максимальное сопротивление (Ом), при котором считается срабатывание прозвонки.
    /// </summary>
    int MaxContinuityResistance { get; set; }

    /// <summary>
    /// Коэффициент делителя ППУ в процентах.
    /// </summary>
    double AcwPpuDividerCoefficientPercent { get; set; }

    /// <summary>
    /// Коэффициент делителя ППУ для DCW в процентах.
    /// </summary>
    double DcwPpuDividerCoefficientPercent { get; set; }

    ISelfTestCheckerMultimeter SelfTestManager { get; set; }
  }
}
