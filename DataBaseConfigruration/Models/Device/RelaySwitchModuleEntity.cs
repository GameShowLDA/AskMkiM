using System.ComponentModel.DataAnnotations.Schema;
using NewCore.Base.Device;
using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using NewCore.Enum;

namespace DataBaseConfiguration.Models.Device
{
  /// <summary>
  /// Класс, представляющий сущность модуля коммутации реле.
  /// </summary>
  public class RelaySwitchModuleEntity : IRelaySwitchModule
  {
    /// <inheritdoc />
    public int Id { get; set; }

    /// <inheritdoc />
    public int NumberChassis { get; set; }

    /// <inheritdoc />
    public int NumberRack { get; set; }

    /// <inheritdoc />
    public int PointCount { get; set; }

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <inheritdoc />
    public int Number { get; set; }

    /// <inheritdoc />
    public string ConnectionDetails { get; set; }

    /// <inheritdoc />
    public DeviceEnum.DeviceType DeviceType => DeviceEnum.DeviceType.RelaySwitchModule;

    /// <inheritdoc />
    public string DeviceClass { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IBusManager BusManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IMeterManager MeterManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IPointManager PointManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IStateManager StateManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IConnectable ConnectableManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IDeviceProtocol DeviceProtocol { get; set; }
  }
}
