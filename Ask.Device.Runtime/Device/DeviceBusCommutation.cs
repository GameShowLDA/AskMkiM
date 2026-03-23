using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Device.Communication.Ethernet;
using NewCore.Base.Device;
using NewCore.Function.DeviceBusCommutation.SelfCheck;
using NewCore.FunctionAdapters.DeviceBusCommutation;

namespace NewCore.Device
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
      Description = "Реализовать описание в NewCore.Device.DeviceBusCommutation";
      DeviceClass = GetType().FullName;
      DeviceType = Ask.Core.Shared.Metadata.Enums.DeviceEnums.DeviceType.SwitchingDevice;

      ConnectableManager = new StateManagerAdapter(this);
      ConnectorManager = new ConnectorManagerAdapter(this);
      CapacitorManager = new CapacitorManagerAdapter(this);
      RelayManager = new RelayManagerAdapter(this);
      ResistorManager = new ResistorManagerAdapter(this);
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
