using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Base.Status;

namespace Ask.Device.Runtime.Base.DeviceProtocol
{
  /// <summary>
  /// Базовый класс устройств с подключением по протоколу UDP/IP.
  /// </summary>
  public abstract class DeviceWithUdpIp : DeviceWithIP, IDevice
  {
    /// <summary>
    /// Инициализирует устройство с подключением по UDP/IP.
    /// </summary>
    protected DeviceWithUdpIp()
    {
      ConnectionInfo = new ConnectionInfoBase(this, ConnectionType.IP_UDP);
    }
  }
}
