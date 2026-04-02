using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule.Capabilities;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule
{
  /// <summary>
  /// Интерфейс для модуля источника напряжения и тока.
  /// </summary>
  public interface IPowerSourceModule : IAttachableDevice
  {

    /// <summary>
    /// Управление подключением и отключением шин.
    /// </summary>
    IBusManager BusManager { get; set; }

    /// <summary>
    /// Управление настройками тока.
    /// </summary>
    ICurrentManager CurrentManager { get; set; }

    /// <summary>
    /// Управление настройками напряжения.
    /// </summary>
    IVoltageManager VoltageManager { get; set; }
    ISelfTestCheckerModuleVoltageCurrentSource SelfTestManager { get; set; }

    /// <summary>
    /// JSON-строка с калибровочными коэффициентами по диапазонам сопротивления
    /// </summary>
    string? ResistanceCalibrationJson { get; set; }
  }
}
