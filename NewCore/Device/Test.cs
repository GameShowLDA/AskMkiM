using NewCore.Base.Device;
using NewCore.Base.Interface.Main;

namespace NewCore.Device
{
  /// <summary>
  /// Класс ManagerChassis представляет устройство с подключением по IP-адресу.
  /// </summary>
  public class Test : DeviceWithIP, IRack
  {
    public Test() { }

    public string Name { get => "Стойка СКМ"; }
    public string Description { get => "Добавить описание сюда"; }
    public int NumberChassis { get; set; }
  }
}