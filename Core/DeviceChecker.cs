using Core.Model;
using Utilities.Models;
using static Utilities.DelegateManager;

namespace Core
{
  /// <summary>
  /// Класс для проверки устройств по различным параметрам.
  /// </summary>
  static public class DeviceChecker
  {
    /// <summary>
    /// Проверяет подключение указанного устройства на основе его типа.
    /// </summary>
    /// <param name="deviceModel">Модель устройства для проверки.</param>
    /// <returns>Задача, представляющая асинхронную операцию, содержащая <c>true</c>, если устройство подключено; иначе <c>false</c>.</returns>
    static public async Task<bool> CheckConnectDevice(DeviceModel deviceModel, MessageDelegate messageDelegate)
    {
      bool result;

      switch (deviceModel.DeviceType)
      {
        case Enum.DeviceEnum.Type.DeviceBusCommutation:
        case Enum.DeviceEnum.Type.ModuleRelayControl:
        case Enum.DeviceEnum.Type.ManagerShassy:
        case Enum.DeviceEnum.Type.ModuleVoltageCurrentSource:
          result = await CheckModule(deviceModel, messageDelegate);
          break;

        case Enum.DeviceEnum.Type.Breakdown:
          result = await CheckBreakdownAsync(deviceModel, messageDelegate);
          break;

        default:
          throw new ArgumentException("Unsupported device type");
      }

      return result;
    }

    static private async Task<bool> CheckModule(DeviceModel deviceModel, MessageDelegate messageDelegate)
    {
      bool pingResult = await Communication.CommunicationManager.PingAsync(deviceModel.Name, deviceModel.IPAddress);
      if (!pingResult)
      {
        await messageDelegate(new ShowMessageModel($"{deviceModel.Name}{deviceModel.Number}", null, "[NO]", ShowMessageModel.ErrorMessage.Item2));
      }
      return pingResult;
    }

    /// <summary>
    /// Проверка подключения к пробойной установке.
    /// </summary>
    /// <param name="deviceModel">Модель мультиметра.</param>
    /// <returns>Результат подключение к мультиметру.</returns>
    static private async Task<bool> CheckBreakdownAsync(DeviceModel deviceModel, MessageDelegate messageDelegate)
    {
      if (deviceModel.Name.ToLower().Contains("79904"))
      {
        return await ConnectToGPT79904Async(messageDelegate);
      }

      return false;
    }

    /// <summary>
    /// Подключение к Keysight.
    /// </summary>
    /// <returns>Результат подключение к мультиметру Keysight.</returns>
    static private async Task<bool> ConnectToGPT79904Async(MessageDelegate messageDelegate)
    {
      var meter = GptLibrary.Model.CreateAsync();
      if (!meter.Connect())
      {
        await messageDelegate(new ShowMessageModel("GPR79904", null, "[NO]", ShowMessageModel.ErrorMessage.Item2));
        return false;
      }

      return true;
    }
  }
}
