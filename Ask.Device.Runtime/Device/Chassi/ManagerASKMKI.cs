using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Device.Runtime.AskMkiM.Function.ManagerChassis;
using Ask.Device.Runtime.Base.DeviceProtocol;

namespace Ask.Device.Runtime.Device.Chassi;

/// <summary>
/// Представляет стойку тестера АСК для новой оболочки конфигурации и испытаний.
/// </summary>
public sealed class ManagerASKMKI : DeviceWithUdpIp, IChassisManager
{
  /// <summary>
  /// Создает стойку тестера АСК с базовыми параметрами конфигурации.
  /// </summary>
  public ManagerASKMKI()
  {
    PowerManager = new PowerManager(this);
    DeviceType = DeviceType.ChassisManager;

    Name = "Тестер АСК";
    Description = "Стойка тестера АСК";
    DeviceClass = GetType().FullName ?? string.Empty;
    BusType = BusStructureEnum.Type.Bus2;
  }

  /// <inheritdoc />
  public IPower PowerManager { get; set; }

  /// <inheritdoc />
  public BusStructureEnum.Type BusType { get; set; }

  /// <summary>
  /// Преобразует стойку АСК в DTO для сохранения в базе данных.
  /// </summary>
  /// <returns>DTO стойки АСК.</returns>
  public ChassisManagerDto Convert()
  {
    return new ChassisManagerDto
    {
      Id = Id,
      Name = Name ?? string.Empty,
      Description = Description ?? string.Empty,
      Number = Number,
      ConnectionDetails = ConnectionDetails ?? string.Empty,
      DeviceType = DeviceType,
      DeviceClass = DeviceClass ?? string.Empty,
      BusType = BusType
    };
  }
}
