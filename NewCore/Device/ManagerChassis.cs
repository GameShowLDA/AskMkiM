using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis.Capabilities;
using Ask.Core.Shared.Metadata.Enums.TranslationEnums;
using NewCore.Base.Device;
using NewCore.Function.ManagerChassis;

namespace NewCore.Device
{
  /// <summary>
  /// Класс ManagerChassis представляет устройство с подключением по IP-адресу.
  /// </summary>
  public class ManagerChassis : DeviceWithIP, IChassisManager
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ManagerChassis"/>.
    /// </summary>
    public ManagerChassis()
    {
      ConnectableManager = new StateManager(this);
      PowerManager = new PowerManager(this);
      DeviceType = Ask.Core.Shared.Metadata.Enums.DeviceEnums.DeviceType.ChassisManager;

      Name = "Тестер АСКМ";
      Description = "Добавить описание сюда";
      DeviceClass = GetType().FullName;
      BusType = BusStructureEnum.Type.Bus2;
    }

    /// <inheritdoc />
    public IPowerManagerChassis PowerManager { get; set; }
    public BusStructureEnum.Type BusType { get; set; }
  }
}