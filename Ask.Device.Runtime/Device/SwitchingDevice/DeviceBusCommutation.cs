using Ask.Core.Shared.DTO.Devices.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.AskMkiM.Base.Commands;
using Ask.Device.Runtime.AskMkiM.Function.DeviceBusCommutation;
using Ask.Device.Runtime.AskMkiM.Function.DeviceBusCommutation.SelfCheck;
using Ask.Device.Runtime.Base.Connected;
using Ask.Device.Runtime.Base.DeviceProtocol;

namespace Ask.Device.Runtime.Device.SwitchingDevice
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

      ConnectableManager = new Transport(this);
      ConnectorManager = new ConnectorManager(this);
      CapacitorManager = new CapacitorManager(this);
      RelayManager = new RelayManager(this);
      ResistorManager = new ResistorManager(this);
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
        DeviceClass = DeviceClass ?? string.Empty
      };
    }
  }
}
