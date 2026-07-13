using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Device.Runtime.Base.Device;
using Ask.Device.Runtime.Function.ManagerChassis;

namespace Ask.Device.Runtime.Device
{
  /// <summary>
  /// Стойка старого тестера АСК без внешнего типа подключения.
  /// </summary>
  public class ManagerASKMKI : DeviceWithASKMKI, IChassisManager
  {
    /// <summary>
    /// Создает стойку старого тестера АСК.
    /// </summary>
    public ManagerASKMKI()
    {
      PowerManager = new PowerManager(this);
      DeviceType = DeviceType.ChassisManager;
      Name = "Тестер АСК";
      Description = "Конфигурация старого тестера АСК";
      DeviceClass = GetType().FullName ?? string.Empty;
      BusType = BusStructureEnum.Type.Bus2;
      ConnectionDetails = string.Empty;
    }

    /// <inheritdoc />
    public IPower PowerManager { get; set; }

    /// <inheritdoc />
    public BusStructureEnum.Type BusType { get; set; }

    /// <inheritdoc />
    public ChassisManagerDto Convert()
    {
      return new ChassisManagerDto
      {
        Id = Id,
        Name = Name ?? string.Empty,
        Description = Description ?? string.Empty,
        Number = Number,
        ConnectionDetails = string.Empty,
        DeviceType = DeviceType,
        DeviceClass = DeviceClass ?? string.Empty,
        BusType = BusType
      };
    }
  }
}
