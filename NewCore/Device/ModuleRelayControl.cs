using NewCore.Base;
using NewCore.Function.ModuleRelayControl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Utilities.LoggerUtility;

namespace NewCore.Device
{
  public class ModuleRelayControl : DeviceWithIP
  {
    public ModuleRelayControl(IPAddress ip) : base(ip)
    {
      Name = "Модуль коммутации реле";
      Description = "Предназначен для коммутации измерительных шин автоматизированной системы контроля к высоковольтным цепям объектов контроля, таких как кабели, жгуты, кабельные сети. Коммутация происходит за счет замыкания реле на шину";
    }

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
