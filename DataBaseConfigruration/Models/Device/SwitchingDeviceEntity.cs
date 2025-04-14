using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using NewCore.Base.Device;
using NewCore.Base.Function.DBC;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using NewCore.Enum;

namespace DataBaseConfiguration.Models.Device
{
  /// <summary>
  /// Класс, представляющий сущность устройства коммутации.
  /// </summary>
  public class SwitchingDeviceEntity : ISwitchingDevice
  {
    /// <inheritdoc />
    public int Id { get; set; }

    /// <inheritdoc />
    public int NumberChassis { get; set; }

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <inheritdoc />
    public int Number { get; set; }

    /// <inheritdoc />
    public string ConnectionDetails { get; set; }

    /// <inheritdoc />
    public DeviceEnum.DeviceType DeviceType => DeviceEnum.DeviceType.SwitchingDevice;

    /// <inheritdoc />
    public string DeviceClass { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IBusDeviceBusCommutation BusManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public ICapacitorDeviceBusCommutation CapacitorManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IConnectorDeviceBusCommutation ConnectorManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IRelayDeviceBusCommutation RelayManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IResistorDeviceBusCommutation ResistorManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public ISelfTestChecker SelfTestManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IConnectable ConnectableManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IDeviceProtocol DeviceProtocol { get; set; }
  }
}
