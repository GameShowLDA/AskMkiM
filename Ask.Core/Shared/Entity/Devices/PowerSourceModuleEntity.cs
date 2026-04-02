using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule.Capabilities;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ask.Core.Shared.Entity.Devices
{
  /// <summary>
  /// Класс, представляющий сущность модуля источника питания.
  /// </summary>
  public class PowerSourceModuleEntity : IPowerSourceModule
  {
    [Key]
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
    public DeviceType DeviceType => DeviceType.PowerSourceModule;

    /// <inheritdoc />
    public string DeviceClass { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IBusManager BusManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public ICurrentManager CurrentManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IStateManager StateManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IVoltageManager VoltageManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IConnectable ConnectableManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IDeviceProtocol DeviceProtocol { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public ISelfTestCheckerModuleVoltageCurrentSource SelfTestManager { get; set; }

    /// <inheritdoc />
    public string? ResistanceCalibrationJson { get; set; }
  }
}
