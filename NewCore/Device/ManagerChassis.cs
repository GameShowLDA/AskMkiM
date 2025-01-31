using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NewCore.Base;
using NewCore.Function.ManagerChassis;
using static Utilities.LoggerUtility;

namespace NewCore.Device
{
  /// <summary>
  /// Класс ManagerChassis представляет устройство с подключением по IP-адресу.
  /// </summary>
  public class ManagerChassis : DeviceWithIP
  {
    public ManagerChassis(IPAddress ip) : base(ip) 
    { 
      Name = "Менджер шасси";
      Description = "Предназначен для управления питанием модулей, управления системой охлаждения, для активации модулей, дежурной активации шасси при включенном питании, дезактивации модулей, отключении шасси при завершении работы.";
    }

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
    public override async Task<bool> IsConnectedAsync()
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