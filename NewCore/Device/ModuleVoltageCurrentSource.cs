using System.Net;
using NewCore.Base;
using NewCore.Function.ModuleVoltageCurrentSource;
using static Utilities.LoggerUtility;

namespace NewCore.Device
{
  public class ModuleVoltageCurrentSource : DeviceWithIP
  {

    public ModuleVoltageCurrentSource(IPAddress iPAddress) : base(iPAddress)
    {
      Name = "Модуль источника напряжения и тока";
      Description = "Предназначен для создания электрических параметров для проверки кабельных изделий, печатных плат, контроля функционирования релейно-коммутационных изделий и другой подобной аппаратуры, проведения испытаний изделий по программам контроля";
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
