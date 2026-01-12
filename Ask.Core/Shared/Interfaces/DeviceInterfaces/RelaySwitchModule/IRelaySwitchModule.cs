using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule

{
  /// <summary>
  /// Интерфейс для модуля коммутации реле.
  /// </summary>
  public interface IRelaySwitchModule : IAttachableDevice
  {
    /// <summary>
    /// Количество точек модуля.
    /// </summary>
    int PointCount { get; set; }

    /// <summary>
    /// Менеджер для управления коммутацией шин.
    /// </summary>
    IBusManager BusManager { get; set; }

    /// <summary>
    /// Менеджер для управления измерителем.
    /// </summary>
    IMeterManager MeterManager { get; set; }

    /// <summary>
    /// Менеджер для управления реле и точками подключения.
    /// </summary>
    IPointManager PointManager { get; set; }

    ISelfTestCheckerModuleRelayControl SelfTestManager { get; set; }
  }
}