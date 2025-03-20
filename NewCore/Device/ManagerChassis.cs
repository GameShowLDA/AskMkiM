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
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ManagerChassis"/>.
    /// </summary>
    public ManagerChassis()
    {
      StateManager = new StateManager(this);
      PowerManager = new PowerManager(this);

      Name = "Тестер АСКМ";
      Description = "Добавить описание сюда";
      DeviceClass = GetType().FullName;
    }

    /// <inheritdoc />
    public IStateManagerChassis StateManager { get; set; }

    /// <inheritdoc />
    public IPowerManagerChassis PowerManager { get; set; }
  }
}