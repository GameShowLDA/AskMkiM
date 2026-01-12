using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;

namespace Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice
{
  /// <summary>
  /// Интерфейс для устройства коммутации.
  /// </summary>
  public interface ISwitchingDevice : IAttachableDevice
  {
    /// <summary>
    /// Задает или возвращает объект для управления конденсаторами.
    /// </summary>
    ICapacitorDeviceBusCommutation CapacitorManager { get; set; }

    /// <summary>
    /// Задает или возвращает объект для управления разъёмами.
    /// </summary>
    IConnectorDeviceBusCommutation ConnectorManager { get; set; }

    /// <summary>
    /// Задает или возвращает объект для управления реле.
    /// </summary>
    IRelayDeviceBusCommutation RelayManager { get; set; }

    /// <summary>
    /// Задает или возвращает объект для управления резисторами.
    /// </summary>
    IResistorDeviceBusCommutation ResistorManager { get; set; }

    /// <summary>
    /// Задаёт иди возвращает объект для самоконтроля устройства.
    /// </summary>
    ISelfTestCheckerDeviceBusCommutation SelfTestManager { get; set; }
  }
}
