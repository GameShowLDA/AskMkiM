using System.Net;
using NewCore.Base.Function.ModuleRelayControl;
using NewCore.Base.Interface.Main;
using NewCore.Communication;
using static NewCore.Enum.DeviceEnum;
using static Utilities.LoggerUtility;

namespace NewCore.Function.ModuleRelayControl
{
  /// <summary>
  /// Управляет подключением и отключением шин модуля коммутации реле (МКР).
  /// </summary>
  public class BusManager : IBusManager
  {
    IRelaySwitchModule _moduleRelayControl { get; set; }

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
    public async Task<bool> ConnectBusAsync(SwitchingBus bus, bool lowVoltage)
    {
      if (!TryGetBusNumber(bus, out int numberBus) || !TryGetBusType(bus, out int typeBus))
      {
        LogError("Ошибка данных шины!");
        return false;
      }

      int typeVoltage = lowVoltage ? numberBus : numberBus + 10;
      DeviceCommand cmd = new DeviceCommand(4, typeBus, typeVoltage, 1);

      LogInformation($"Команда: \"{cmd.ToString()}\". Замыкаем {(lowVoltage ? "низковольтную" : "высоковольтную")} шину {bus}.");
      await _moduleRelayControl.DeviceProtocol.QueryAsync(cmd.ToString(), timeout: 1000);
      return true;
    }

    /// <summary>
    /// Отключение шин МКР.
    /// </summary>
    /// <param name="bus">Замыкаемая шина.</param>
    /// <param name="lowVoltage">true - низковольтная шина, false - высоковольтная.</param>
    /// <returns>Результат замыкания шины.</returns>
    public async Task<bool> DisconnectBusAsync(SwitchingBus bus, bool lowVoltage)
    {
      if (!TryGetBusNumber(bus, out int numberBus) || !TryGetBusType(bus, out int typeBus))
      {
        LogError("Ошибка данных шины!");
        return false;
      }

      int typeVoltage = lowVoltage ? numberBus : numberBus + 10;
      DeviceCommand cmd = new DeviceCommand(4, typeBus, typeVoltage, 2);

      LogInformation($"Команда: \"{cmd.ToString()}\". Размыкаем {(lowVoltage ? "низковольтную" : "высоковольтную")} шину {bus}.");
      await _moduleRelayControl.DeviceProtocol.QueryAsync(new DeviceCommand(4, typeBus, typeVoltage, 2).ToString());
      return true;
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
