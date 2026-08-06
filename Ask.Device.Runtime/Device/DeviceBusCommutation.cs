using Ask.Core.Shared.DTO.Devices.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Base.Device;
using Ask.Device.Runtime.Commands;
using Ask.Device.Runtime.Function.Base.Connected;
using Ask.Device.Runtime.Function.DeviceBusCommutation.SelfCheck;

namespace Ask.Device.Runtime.Device
{
  /// <summary>
  /// Устройство коммутации шин, обеспечивающее подключение различных измерителей системы.
  /// </summary>
  public class DeviceBusCommutation : DeviceWithUdpIp, ISwitchingDevice
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DeviceBusCommutation"/>.
    /// </summary>
    public DeviceBusCommutation()
    {
      Name = "Устройство УКШ";
      Description = "Реализовать описание в Ask.Device.Runtime.Device.DeviceBusCommutation";
      DeviceClass = GetType().FullName;
      DeviceType = DeviceType.SwitchingDevice;
      ConnectedProfile.Initialize = new DeviceCommand(1, 0, 0, 0).ToString();
      ConnectedProfile.Reset = new DeviceCommand(2, 1, 0, 0).ToString();

      ConnectableManager = new Transport(this);
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

    public SwitchingDeviceDto Convert()
    {
      return new SwitchingDeviceDto
      {
        Id = Id,
        NumberChassis = NumberChassis,
        Name = Name ?? string.Empty,
        Description = Description ?? string.Empty,
        Number = Number,
        ConnectionDetails = ConnectionDetails ?? string.Empty,
        DeviceType = DeviceType,
        DeviceClass = DeviceClass ?? string.Empty,
        IsHardwareFailureSimulationEnabled = IsHardwareFailureSimulationEnabled
      };
    }
  }
}
