using NewCore.Base.Function.DBC;
using NewCore.Base.Interface.Additionally;

namespace NewCore.Base.Interface.Main
{
  /// <summary>
  /// Интерфейс для устройства коммутации.
  /// </summary>
  public interface ISwitchingDevice : IAttachableDevice
  {
    /// <summary>
    /// Задает или возвращает объект для управления конденсаторами.
    /// </summary>
    public ICapacitorDeviceBusCommutation CapacitorManager { get; set; }

    /// <summary>
    /// Задает или возвращает объект для управления разъёмами.
    /// </summary>
    public IConnectorDeviceBusCommutation ConnectorManager { get; set; }

    /// <summary>
    /// Задает или возвращает объект для управления реле.
    /// </summary>
    public IRelayDeviceBusCommutation RelayManager { get; set; }

    /// <summary>
    /// Задает или возвращает объект для управления резисторами.
    /// </summary>
    public IResistorDeviceBusCommutation ResistorManager { get; set; }

    /// <summary>
    /// Задаёт иди возвращает объект для самоконтроля устройства.
    /// </summary>
    public ISelfTestCheckerDeviceBusCommutation SelfTestManager { get; set; }
  }
}
