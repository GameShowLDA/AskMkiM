using NewCore.Base.Device;
using NewCore.Base.Function.DBC;
using NewCore.Base.Interface.Main;
using NewCore.Enum;
using NewCore.Function.DeviceBusCommutation;

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
      Name = "Устройство коммутации шин";
      Description = "Реализовать описание в NewCore.Device.DeviceBusCommutation";
      DeviceClass = GetType().FullName;
      DeviceType = DeviceEnum.DeviceType.SwitchingDevice;

      CapacitorManager = new CapacitorManager(this);
      ConnectorManager = new ConnectorManager(this);
      RelayManager = new RelayManager(this);
      ResistorManager = new ResistorManager(this);
      ConnectableManager = new StateManager(this);
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
    public ISelfTestChecker SelfTestManager { get; set; }

    /// <summary>
    /// Устанавливает или возвращает номер шасси.
    /// </summary>
    public int NumberChassis { get; set; }
  }
}
