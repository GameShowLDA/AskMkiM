using NewCore.Communication;
using NewCore.Device;
using System.Net;
using static NewCore.Enum.DeviceEnum;
using static Utilities.LoggerUtility;

namespace NewCore.Function.ModuleVoltageCurrentSource
{
  /// <summary>
  /// Функции управления МИНТом.
  /// </summary>
  public class Functions
  {
    public Functions(Device.ModuleVoltageCurrentSource moduleVoltageCurrentSource) => _moduleVoltageCurrentSource = moduleVoltageCurrentSource;

    Device.ModuleVoltageCurrentSource _moduleVoltageCurrentSource { get; set; }

    /// <summary>
    /// Устанавливает дискретное значение напряжения на МИНТ.
    /// </summary>
    /// <param name="_moduleVoltageCurrentSource.IPAddress">IP-адрес МИНТ, на который отправляется команда.</param>
    /// <param name="voltageSources">Источник напряжения.</param>
    /// <returns>Асинхронная задача.</returns>
    public async Task SetSourceVoltageAsync(VoltageSources voltageSources)
    {
      LogInformation($"Устанавливаем иточник питания {(voltageSources == VoltageSources.Supply12V ? "12В" : "5В")}");
      await DeviceCommandSender.SendCommandAsync(_moduleVoltageCurrentSource.IPAddress, new DeviceCommand(9, voltageSources == VoltageSources.Supply12V ? 1 : 0));
    }

    /// <summary>
    /// Устанавливает дискретное значение напряжения на МИНТ.
    /// </summary>
    /// <param name="_moduleVoltageCurrentSource.IPAddress">IP-адрес МИНТ, на который отправляется команда.</param>
    /// <param name="integerPart">Целая часть напряжения.</param>
    /// <param name="decimalPart">Дробная часть напряжения.</param>
    /// <returns>Асинхронная задача.</returns>
    public async Task SetVoltageLevelAsync(int integerPart, int decimalPart)
    {
      LogInformation($"Устанавливаем напряжение {integerPart}.{decimalPart} В ({new DeviceCommand(3, integerPart, decimalPart).ToString()})");
      await DeviceCommandSender.SendCommandAsync(_moduleVoltageCurrentSource.IPAddress, new DeviceCommand(3, integerPart, decimalPart));
    }

    /// <summary>
    /// Устанавливает дискретное значение тока на МИНТ.
    /// </summary>
    /// <param name="_moduleVoltageCurrentSource.IPAddress">IP-адрес МИНТ, на который отправляется команда.</param>
    /// <param name="integerPart">Целая часть значения тока.</param>
    /// <param name="decimalPart">Дробная часть значения тока.</param>
    /// <returns>Асинхронная задача.</returns>
    public async Task SetCurrentLevelAsync(int integerPart, int decimalPart)
    {
      LogInformation($"Устанавливаем ток {integerPart}.{decimalPart} мА ({new DeviceCommand(4, integerPart, decimalPart).ToString()})");
      await DeviceCommandSender.SendCommandAsync(_moduleVoltageCurrentSource.IPAddress, new DeviceCommand(4, integerPart, decimalPart));
    }

    /// <summary>
    /// Подключить шину МИНТ к положительному полюсу.
    /// </summary>
    /// <param name="_moduleVoltageCurrentSource.IPAddress">Ip устройства.</param>
    /// <param name="bus">Замыкаемая шина.</param>
    /// <returns>Результат замыкания шины.</returns>
    public async Task<bool> ConnectBusToPositiveAsync(SwitchingBus bus)
    {
      Tuple<int, int> partialComand;
      BusParameters.TryGetValue(bus, out partialComand);
      LogInformation($"МИНТ: Подключение шины {bus.ToString()} к + ({new DeviceCommand(5, partialComand.Item1, partialComand.Item2, 1).ToString()})");
      await DeviceCommandSender.SendCommandAsync(_moduleVoltageCurrentSource.IPAddress, new DeviceCommand(5, partialComand.Item1, partialComand.Item2, 1));
      return true;
    }

    /// <summary>
    /// Подключить шину МИНТ к отрицательному полюсу.
    /// </summary>
    /// <param name="_moduleVoltageCurrentSource.IPAddress">Ip устройства.</param>
    /// <param name="bus">Замыкаемая шина.</param>
    /// <returns>Результат замыкания шины.</returns>
    public async Task<bool> ConnectBusToNegativeAsync(SwitchingBus bus)
    {
      Tuple<int, int> partialComand;
      BusParameters.TryGetValue(bus, out partialComand);
      LogInformation($"МИНТ: Подключение шины {bus.ToString()} к - ({new DeviceCommand(6, partialComand.Item1, partialComand.Item2, 1).ToString()})");
      await DeviceCommandSender.SendCommandAsync(_moduleVoltageCurrentSource.IPAddress, new DeviceCommand(6, partialComand.Item1, partialComand.Item2, 1));
      return true;
    }

    /// <summary>
    /// Отключает шину МИНТ от положительного полюса.
    /// </summary>
    /// <param name="_moduleVoltageCurrentSource.IPAddress">Ip устройства.</param>
    /// <param name="bus">Отключаемая шина.</param>
    /// <returns>Результат замыкания шины.</returns>
    public async Task<bool> DisconnectBusToPositiveAsync(SwitchingBus bus)
    {
      Tuple<int, int> partialComand;
      BusParameters.TryGetValue(bus, out partialComand);
      LogInformation($"МИНТ: Отключение шины {bus.ToString()} от + ({new DeviceCommand(5, partialComand.Item1, partialComand.Item2, 2).ToString()})");
      await DeviceCommandSender.SendCommandAsync(_moduleVoltageCurrentSource.IPAddress, new DeviceCommand(5, partialComand.Item1, partialComand.Item2, 2));
      return true;
    }

    /// <summary>
    /// Отключает шину МИНТ от отрицательному полюса.
    /// </summary>
    /// <param name="_moduleVoltageCurrentSource.IPAddress">Ip устройства.</param>
    /// <param name="bus">Отключаемая шина.</param>
    /// <returns>Результат замыкания шины.</returns>
    public async Task<bool> DisconnectBusToNegativeAsync(SwitchingBus bus)
    {
      Tuple<int, int> partialComand;
      BusParameters.TryGetValue(bus, out partialComand);
      LogInformation($"МИНТ: Отключение шины {bus.ToString()} от - ({new DeviceCommand(6, partialComand.Item1, partialComand.Item2, 2).ToString()})");
      await DeviceCommandSender.SendCommandAsync(_moduleVoltageCurrentSource.IPAddress, new DeviceCommand(6, partialComand.Item1, partialComand.Item2, 2));
      return true;
    }

    /// <summary>
    /// Устанавливает ограничение выдаваемого тока для ПИН.
    /// </summary>
    /// <param name="_moduleVoltageCurrentSource.IPAddress">Ip устройства.</param>
    /// <param name="current">Ток в мА.</param>
    /// <returns>Результат установки тока.</returns>
    public async Task<bool> LimitationOfTheOutputCurrent(int current)
    {
      LogInformation($"МИНТ: Установка ограничения тока в {current}мА от - ({new DeviceCommand(10, current).ToString()})");
      await DeviceCommandSender.SendCommandAsync(_moduleVoltageCurrentSource.IPAddress, new DeviceCommand(10, current));
      return true;
    }

    /// <summary>
    /// Инициализация модуля коммутации реле.
    /// </summary>
    /// <returns>Возвращает ответ, получен ли ответ от инициализации.</returns>
    public async Task<(bool Connect, string Answer)> Initialize()
    {
      DeviceCommand cmd = new DeviceCommand(1, 0, 0, 0);
      string result = await DeviceCommandSender.SendCommandAsync(_moduleVoltageCurrentSource.IPAddress, cmd, 2000).ConfigureAwait(true);
      return result == "2.1" ? (true, string.Empty) : (false, result);
    }
  }
}
