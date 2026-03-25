using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Rack;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ask.Core.Shared.Entity.Devices
{
  /// <summary>
  /// Класс, представляющий сущность стойка коммутационная.
  /// </summary>
  public class RackEntity : IRack
  {
    [Key]
    /// <inheritdoc />
    public int Id { get; set; }

    /// <inheritdoc />
    public string Name { get; set; }

    /// <inheritdoc />
    public string Description { get; set; }

    /// <inheritdoc />
    public int Number { get; set; }

    /// <inheritdoc />
    public int NumberChassis { get; set; }

    /// <inheritdoc />
    public string ConnectionDetails { get; set; }

    /// <inheritdoc />
    public DeviceType DeviceType => DeviceType.Rack;

    /// <inheritdoc />
    public string DeviceClass { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IConnectable ConnectableManager { get; set; }

    /// <inheritdoc />
    [NotMapped]
    public IDeviceProtocol DeviceProtocol { get; set; }

    [NotMapped]
    public BusStructureEnum.Type BusType { get; set; }
  }
}
