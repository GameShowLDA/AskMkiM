using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Capabilities;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using Ask.Device.Runtime.AskMkiM.Base.Commands;
using Ask.Device.Runtime.AskMkiM.Function.ManagerChassis;
using Ask.Device.Runtime.Base.Connected;
using Ask.Device.Runtime.Base.DeviceProtocol;

namespace Ask.Device.Runtime.Device.Chassi
{
  /// <summary>
  /// Класс ManagerChassis представляет устройство с подключением по IP-адресу.
  /// </summary>
  public class ManagerChassis : DeviceWithUdpIp, IChassisManager
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ManagerChassis"/>.
    /// </summary>
    public ManagerChassis()
    {
      ConnectableManager = new Transport(this);
      PowerManager = new PowerManager(this);
      DeviceType = DeviceType.ChassisManager;
      ConnectedProfile.Initialize = new DeviceCommand(1, 0, 0, 0).ToString();
      ConnectedProfile.Reset = new DeviceCommand(2, 1, 0, 0).ToString();

      Name = "Тестер АСКМ";
      Description = "Добавить описание сюда";
      DeviceClass = GetType().FullName;
      BusType = BusStructureEnum.Type.Bus2;
    }

    /// <inheritdoc />
    public IPower PowerManager { get; set; }
    public BusStructureEnum.Type BusType { get; set; }

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
