using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NewCore.Base;
using NewCore.Function.ManagerChassis;
using NewCore.Interface;
using static Utilities.LoggerUtility;

namespace NewCore.Device
{
  /// <summary>
  /// Класс ManagerChassis представляет устройство с подключением по IP-адресу.
  /// </summary>
  public class Test : DeviceWithIP, IChassisManager
  {
    public Test(IPAddress ip) : base(ip) { }
    public Test() { }

    public string Name { get => "Тест 2";}
    public string Description { get => "Добавить описание сюда";}
    
 

    /// <summary>
    /// Проверяет соединение с устройством.
    /// </summary>
    /// <returns>
    /// Возвращает <see cref="bool"/>, указывающий на наличие соединения:
    /// <c>true</c> — если соединение установлено, <c>false</c> — в противном случае.
    /// </returns>
    /// <exception cref="Exception">Выбрасывается, если произошла непредвиденная ошибка при проверке соединения.</exception>
    public override async Task<bool> Initialize()
    {
      try
      {
        return true;
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при проверке соединения: {ex.Message}");
        return false;
      }
    }
  }
}