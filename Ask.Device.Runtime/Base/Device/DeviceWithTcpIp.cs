using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Function.Base.Status;

namespace Ask.Device.Runtime.Base.Device
{
  /// <summary>
  /// Базовый класс устройств с подключением по протоколу TCP/IP.
  /// </summary>
  public abstract class DeviceWithTcpIp : DeviceWithIP, IDevice
  {
    /// <summary>
    /// Инициализирует устройство с подключением по TCP/IP.
    /// </summary>
    protected DeviceWithTcpIp()
    {
      ConnectionInfo = new ConnectionInfoBase(this, ConnectionType.IP_TCP);
    }
  }
}