using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using NewCore.Base.DeviceResponses;
using NewCore.Communication;
using static Ask.LogLib.LoggerUtility;

namespace NewCore.Function.ModuleRelayControl
{
  /// <summary>
  /// Управляет подключением и отключением шин модуля коммутации реле (МКР).
  /// </summary>
  public class BusManager : IBusManager
  {
    private IRelaySwitchModule _moduleRelayControl { get; set; }

    /// <summary>
    /// Создаёт новый экземпляр класса <see cref="BusManager"/>.
    /// </summary>
    /// <param name="moduleRelayControl">Экземпляр интерфейса модуля реле.</param>
    public BusManager(IRelaySwitchModule moduleRelayControl) => _moduleRelayControl = moduleRelayControl;

    /// <summary>
    /// Подключить шину МКР.
    /// </summary>
    /// <param name="bus">Замыкаемая шина.</param>
    /// <param name="lowVoltage">true - низковольтная шина, false - высоковольтная.</param>
    /// <returns>Результат замыкания шины.</returns>
    public async Task<bool> ConnectBusAsync(SwitchingBus bus, IUserInteractionService? userMessageService = null)
    {
      if (!TryGetBusNumber(bus, out int numberBus) || !TryGetBusType(bus, out int typeBus))
      {
        LogError("Ошибка данных шины!", isDeviceLog: true);
        return false;
      }

      if (await ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      int typeVoltage =  numberBus;
      DeviceCommand cmd = new DeviceCommand(4, typeBus, typeVoltage, 1);
      string commandText = cmd.ToString();

      LogInformation($"Команда: \"{commandText}\". Замыкаем шину {bus}.", isDeviceLog: true);

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        string response = await _moduleRelayControl.DeviceProtocol.QueryAsync(commandText, timeout: 1000);
        var parsed = BaseResponse.FromJson(response);

        if (parsed?.Answer.Contains($"4.{typeBus}.{typeVoltage}") ?? false)
        {
          return true;
        }

        LogWarning($"Ответ не получен или некорректный. Попытка {attempt}.", isDeviceLog: true);
        await Task.Delay(100);
      }

      LogError("Не удалось получить корректный ответ от устройства.", isDeviceLog: true);
      return false;
    }

    /// <summary>
    /// Отключение шин МКР.
    /// </summary>
    /// <param name="bus">Замыкаемая шина.</param>
    /// <param name="lowVoltage">true - низковольтная шина, false - высоковольтная.</param>
    /// <returns>Результат замыкания шины.</returns>
    public async Task<bool> DisconnectBusAsync(SwitchingBus bus, IUserInteractionService? userMessageService = null)
    {
      if (!TryGetBusNumber(bus, out int numberBus) || !TryGetBusType(bus, out int typeBus))
      {
        LogError("Ошибка данных шины!", isDeviceLog: true);
        return false;
      }

      if (await ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      int typeVoltage = numberBus;
      DeviceCommand cmd = new DeviceCommand(4, typeBus, typeVoltage, 2);
      string commandText = cmd.ToString();

      LogInformation($"Команда: \"{commandText}\". Размыкаем шину {bus}.", isDeviceLog: true);

      for (int attempt = 1; attempt <= 2; attempt++)
      {
        string response = await _moduleRelayControl.DeviceProtocol.QueryAsync(commandText, timeout: 1000);
        var parsed = BaseResponse.FromJson(response);

        if (parsed?.Answer == $"4.{typeBus}.{typeVoltage}.2")
        {
          return true;
        }

        LogWarning($"Ответ не получен или некорректный. Попытка {attempt}.", isDeviceLog: true);
        await Task.Delay(100);
      }

      LogError("Не удалось получить корректный ответ от устройства.", isDeviceLog: true);
      return false;
    }

    /// <summary>
    /// Пытается получить номер шины на основе значения перечисления SwitchingBus.
    /// </summary>
    /// <param name="bus">Значение перечисления SwitchingBus.</param>
    /// <param name="busNumber">Выходной параметр, который содержит номер шины, если операция успешна.</param>
    /// <returns>Возвращает true, если номер шины успешно получен; в противном случае - false.</returns>
    public bool TryGetBusNumber(SwitchingBus bus, out int busNumber)
    {
      string busName = bus.ToString();
      busNumber = -1;

      foreach (char ch in busName)
      {
        if (char.IsDigit(ch))
        {
          return int.TryParse(busName.Substring(busName.IndexOf(ch)), out busNumber);
        }
      }

      return false;
    }

    /// <summary>
    /// Пытается преобразовать шину в тип (A, B, AB) на основе значения перечисления SwitchingBus.
    /// </summary>
    /// <param name="bus">Значение перечисления SwitchingBus.</param>
    /// <param name="busType">Выходной параметр, который содержит тип шины (1 для A, 2 для B, 3 для AB), если операция успешна.</param>
    /// <returns>Возвращает true, если тип шины успешно получен; в противном случае - false.</returns>
    public bool TryGetBusType(SwitchingBus bus, out int busType)
    {
      busType = -1;

      if (bus is SwitchingBus.A1 || bus is SwitchingBus.A2 || bus is SwitchingBus.A3 || bus is SwitchingBus.A4)
      {
        busType = 1;
      }
      else if (bus is SwitchingBus.B1 || bus is SwitchingBus.B2 || bus is SwitchingBus.B3 || bus is SwitchingBus.B4)
      {
        busType = 2;
      }
      else if (bus is SwitchingBus.AB1 || bus is SwitchingBus.AB2 || bus is SwitchingBus.AB3 || bus is SwitchingBus.AB4)
      {
        busType = 3;
      }

      return busType != -1;
    }

  }
}
