using NewCore.Base.Device;
using NewCore.Base.Function.ManagerChassis;
using NewCore.Base.Interface.Main;
using NewCore.Function.ManagerChassis;

namespace NewCore.Device
{
  /// <summary>
  /// Класс ManagerChassis представляет устройство с подключением по IP-адресу.
  /// </summary>
  public class ManagerChassis : DeviceWithIP, IChassisManager
  {
    public ManagerChassis()
    {
      StateManager = new StateManager(this);
      PowerManager = new PowerManager(this);
    }

    public string Name { get => "Тестер АСКМ"; }
    public string Description { get => "Добавить описание сюда"; }

    public string DeviceClass { get => GetType().FullName; }
    public IStateManagerChassis StateManager { get; set; }
    public IPowerManagerChassis PowerManager { get; set; }
  }
}