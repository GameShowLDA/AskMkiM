using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;

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
    
    /// <summary>
    /// Тип структурной шины.
    /// </summary>
    SwitchingBusNew BusType { get; set; }

    /// <summary>
    /// Сопротивление коммутатора.
    /// </summary>
    double SwitchResistance { get; set; }

    /// <summary>
    /// Собственная ёмкость коммутатора.
    /// </summary>
    double SwitchCapacitance { get; set; }

    ISelfTestCheckerModuleRelayControl SelfTestManager { get; set; }
  }
}
