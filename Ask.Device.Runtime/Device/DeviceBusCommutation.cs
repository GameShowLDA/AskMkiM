using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Device.Communication.Ethernet;
using Ask.Device.Runtime.Base.Device;
using Ask.Device.Runtime.Function.DeviceBusCommutation.SelfCheck;

namespace Ask.Device.Runtime.Device
{
  /// <summary>
  /// Устройство коммутации шин, обеспечивающее подключение различных измерителей системы.
  /// </summary>
  public class DeviceBusCommutation : DeviceWithIP, ISwitchingDevice
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DeviceBusCommutation"/>.
    /// </summary>
    public DeviceBusCommutation()
    {
      Name = "Устройство УКШ";
      Description = "Реализовать описание в Ask.Device.Runtime.Device.DeviceBusCommutation";
      DeviceClass = GetType().FullName;
      DeviceType = Ask.Core.Shared.Metadata.Enums.DeviceEnums.DeviceType.SwitchingDevice;

      ConnectableManager = new Function.DeviceBusCommutation.StateManager(this);
      ConnectorManager = new Function.DeviceBusCommutation.ConnectorManager(this);
      CapacitorManager = new Function.DeviceBusCommutation.CapacitorManager(this);
      RelayManager = new Function.DeviceBusCommutation.RelayManager(this);
      ResistorManager = new Function.DeviceBusCommutation.ResistorManager(this);
      SelfTestManager = new SelfTestManager(this);
    }

    /// <inheritdoc />
    public ICapacitorDeviceBusCommutation CapacitorManager { get; set; }

    /// <inheritdoc />
    public IConnectorDeviceBusCommutation ConnectorManager { get; set; }

    /// <inheritdoc />
    public IRelayDeviceBusCommutation RelayManager { get; set; }

    /// <inheritdoc />
    public IResistorDeviceBusCommutation ResistorManager { get; set; }

    /// <inheritdoc />
    public ISelfTestCheckerDeviceBusCommutation SelfTestManager { get; set; }

    /// <summary>
    /// Устанавливает или возвращает номер шасси.
    /// </summary>
    public int NumberChassis { get; set; }
  }
}
