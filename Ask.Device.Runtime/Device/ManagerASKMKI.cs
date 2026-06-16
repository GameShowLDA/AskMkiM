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
  /// Представляет legacy-тестер АСК-МКИ без выбора типа подключения.
  /// </summary>
  public class ManagerASKMKI : DeviceWithASKMKI, IChassisManager
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ManagerASKMKI"/>.
    /// </summary>
    public ManagerASKMKI()
    {
      PowerManager = new PowerManager(this);
      DeviceType = DeviceType.ChassisManager;
      Name = "Тестер АСК";
      Description = "Legacy-конфигурация оборудования АСК-МКИ";
      DeviceClass = GetType().FullName ?? string.Empty;
      BusType = BusStructureEnum.Type.Bus2;
    }

    /// <inheritdoc />
    public IPower PowerManager { get; set; }

    /// <inheritdoc />
    public BusStructureEnum.Type BusType { get; set; }

    /// <summary>
    /// Преобразует runtime-модель legacy-тестера АСК-МКИ в DTO стойки.
    /// </summary>
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
}
