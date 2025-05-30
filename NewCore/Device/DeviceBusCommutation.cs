using NewCore.Base.Device;
using NewCore.Base.Function.DBC;
using NewCore.Base.Interface.Additionally;
using NewCore.Base.Interface.Main;
using NewCore.Enum;
using NewCore.Function.DeviceBusCommutation;
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
      DeviceType = DeviceEnum.DeviceType.SwitchingDevice;

      CapacitorManager = new CapacitorManagerAdapter(this);
      ConnectorManager = new ConnectorManagerAdapter(this);
      RelayManager = new RelayManagerAdapter(this);
      ResistorManager = new ResistorManagerAdapter(this);
      ConnectableManager = new StateManagerAdapter(this);
      SelfTestManager = new Function.DeviceBusCommutation.SelfCheck.SelfTestManager(this);
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
