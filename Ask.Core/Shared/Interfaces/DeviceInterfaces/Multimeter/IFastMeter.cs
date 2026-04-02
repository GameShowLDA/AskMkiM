using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter.Capabilities;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter
{
  /// <summary>
  /// Интерфейс для быстрого измерителя.
  /// </summary>
  public interface IFastMeter : IAttachableDevice
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
    /// Максимальное сопротивление (Ом), при котором считается срабатывание прозвонки.
    /// </summary>
    int MaxContinuityResistance { get; set; }
  }
}
