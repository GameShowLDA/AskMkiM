using System.Net;
using NewCore.Base;
using NewCore.Enum;
using NewCore.Function.ModuleRelayControl;
using NewCore.Interface;
using static Utilities.LoggerUtility;

namespace NewCore.Device
{
  public class ModuleRelayControl : DeviceWithIP, IRelaySwitchModule
  {
    public ModuleRelayControl(IPAddress iPAddress) : base(iPAddress) { }
    public ModuleRelayControl() { }

    public Functions Functions => new Functions(this);

    public int Number { get; set; }
    public string ConnectionDetails { get; set; }

    public DeviceEnum.DeviceType DeviceType => DeviceEnum.DeviceType.ChassisManager;

    public int NumberChassis { get; set; }
    public int PointCount { get; set; }

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
