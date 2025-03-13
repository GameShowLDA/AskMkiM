using System.Net;
using NewCore.Base;
using NewCore.Function.ManagerChassis;
using NewCore.Interface;
using static Utilities.LoggerUtility;

namespace NewCore.Device
{
  /// <summary>
  /// Класс ManagerChassis представляет устройство с подключением по IP-адресу.
  /// </summary>
  public class ManagerChassis : DeviceWithIP, IChassisManager
  {
    public ManagerChassis(IPAddress ip) : base(ip) { }
    public ManagerChassis() { }

    public string Name { get => "Тестер АСКМ"; }
    public string Description { get => "Добавить описание сюда"; }

    /// <summary>
    /// Предоставляет доступ к функциям устройства.
    /// </summary>
    public Functions Functions => new Functions(this);

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
        var (isConnected, answer) = await Functions.Initialize();

        if (!isConnected)
        {
          LogWarning($"Соединение с устройством не установлено. ({answer})");
        }
        else
        {
          LogInformation("Соединение с устройством успешно установлено.");
        }

        return isConnected;
      }
      catch (Exception ex)
      {
        LogError($"Ошибка при проверке соединения: {ex.Message}");
        return false;
      }
    }
  }
}