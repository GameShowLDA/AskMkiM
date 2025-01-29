using System.Net;
using Core.Communication;
using static Core.ModuleVoltageCurrentSource.Enums;
using static Utilities.LoggerUtility;

namespace Core.ModuleVoltageCurrentSource
{
  /// <summary>
  /// Функции управления МИНТом.
  /// </summary>
  public static class Functions
  {

    /// <summary>
    /// Устанавливает дискретное значение напряжения на МИНТ.
    /// </summary>
    /// <param name="ip">IP-адрес МИНТ, на который отправляется команда.</param>
    /// <param name="voltageSources">Источник напряжения.</param>
    /// <returns>Асинхронная задача.</returns>
    public static async Task SetSourceVoltageAsync(IPAddress ip, VoltageSources voltageSources)
    {
      LogInformation($"Устанавливаем иточник питания {(voltageSources == VoltageSources.Supply12V ? "12В" : "5В")}");
      await CommunicationManager.SendCommandAsync(ip, new Command(9, voltageSources == VoltageSources.Supply12V ? 1 : 0));
    }

    /// <summary>
    /// Устанавливает дискретное значение напряжения на МИНТ.
    /// </summary>
    /// <param name="ip">IP-адрес МИНТ, на который отправляется команда.</param>
    /// <param name="integerPart">Целая часть напряжения.</param>
    /// <param name="decimalPart">Дробная часть напряжения.</param>
    /// <returns>Асинхронная задача.</returns>
    public static async Task SetVoltageLevelAsync(IPAddress ip, int integerPart, int decimalPart)
    {
      LogInformation($"Устанавливаем напряжение {integerPart}.{decimalPart} В ({new Command(3, integerPart, decimalPart).ToString()})");
      await CommunicationManager.SendCommandAsync(ip, new Command(3, integerPart, decimalPart));
    }

    /// <summary>
    /// Устанавливает дискретное значение тока на МИНТ.
    /// </summary>
    /// <param name="ip">IP-адрес МИНТ, на который отправляется команда.</param>
    /// <param name="integerPart">Целая часть значения тока.</param>
    /// <param name="decimalPart">Дробная часть значения тока.</param>
    /// <returns>Асинхронная задача.</returns>
    public static async Task SetCurrentLevelAsync(IPAddress ip, int integerPart, int decimalPart)
    {
      LogInformation($"Устанавливаем ток {integerPart}.{decimalPart} мА ({new Command(4, integerPart, decimalPart).ToString()})");
      await CommunicationManager.SendCommandAsync(ip, new Command(4, integerPart, decimalPart));
    }

    /// <summary>
    /// Подключить шину МИНТ к положительному полюсу.
    /// </summary>
    /// <param name="ip">Ip устройства.</param>
    /// <param name="bus">Замыкаемая шина.</param>
    /// <returns>Результат замыкания шины.</returns>
    public static async Task<bool> ConnectBusToPositiveAsync(IPAddress ip, BusModuleVoltageCurrentSource bus)
    {
      Tuple<int, int> partialComand;
      KeyValuePairsModuleVoltageCurrentSource.TryGetValue(bus, out partialComand);
      LogInformation($"МИНТ: Подключение шины {bus.ToString()} к + ({new Command(5, partialComand.Item1, partialComand.Item2, 1).ToString()})");
      await CommunicationManager.SendCommandAsync(ip, new Command(5, partialComand.Item1, partialComand.Item2, 1));
      return true;
    }

    /// <summary>
    /// Подключить шину МИНТ к отрицательному полюсу.
    /// </summary>
    /// <param name="ip">Ip устройства.</param>
    /// <param name="bus">Замыкаемая шина.</param>
    /// <returns>Результат замыкания шины.</returns>
    public static async Task<bool> ConnectBusToNegativeAsync(IPAddress ip, BusModuleVoltageCurrentSource bus)
    {
      Tuple<int, int> partialComand;
      KeyValuePairsModuleVoltageCurrentSource.TryGetValue(bus, out partialComand);
      LogInformation($"МИНТ: Подключение шины {bus.ToString()} к - ({new Command(6, partialComand.Item1, partialComand.Item2, 1).ToString()})");
      await CommunicationManager.SendCommandAsync(ip, new Command(6, partialComand.Item1, partialComand.Item2, 1));
      return true;
    }

    /// <summary>
    /// Отключает шину МИНТ от положительного полюса.
    /// </summary>
    /// <param name="ip">Ip устройства.</param>
    /// <param name="bus">Отключаемая шина.</param>
    /// <returns>Результат замыкания шины.</returns>
    public static async Task<bool> DisconnectBusToPositiveAsync(IPAddress ip, BusModuleVoltageCurrentSource bus)
    {
      Tuple<int, int> partialComand;
      KeyValuePairsModuleVoltageCurrentSource.TryGetValue(bus, out partialComand);
      LogInformation($"МИНТ: Отключение шины {bus.ToString()} от + ({new Command(5, partialComand.Item1, partialComand.Item2, 2).ToString()})");
      await CommunicationManager.SendCommandAsync(ip, new Command(5, partialComand.Item1, partialComand.Item2, 2));
      return true;
    }

    /// <summary>
    /// Отключает шину МИНТ от отрицательному полюса.
    /// </summary>
    /// <param name="ip">Ip устройства.</param>
    /// <param name="bus">Отключаемая шина.</param>
    /// <returns>Результат замыкания шины.</returns>
    public static async Task<bool> DisconnectBusToNegativeAsync(IPAddress ip, BusModuleVoltageCurrentSource bus)
    {
      Tuple<int, int> partialComand;
      KeyValuePairsModuleVoltageCurrentSource.TryGetValue(bus, out partialComand);
      LogInformation($"МИНТ: Отключение шины {bus.ToString()} от - ({new Command(6, partialComand.Item1, partialComand.Item2, 2).ToString()})");
      await CommunicationManager.SendCommandAsync(ip, new Command(6, partialComand.Item1, partialComand.Item2, 2));
      return true;
    }

    /// <summary>
    /// Устанавливает ограничение выдаваемого тока для ПИН.
    /// </summary>
    /// <param name="ip">Ip устройства.</param>
    /// <param name="current">Ток в мА.</param>
    /// <returns>Результат установки тока.</returns>
    public static async Task<bool> LimitationOfTheOutputCurrent(IPAddress ip, int current)
    {
      LogInformation($"МИНТ: Установка ограничения тока в {current}мА от - ({new Command(10, current).ToString()})");
      await CommunicationManager.SendCommandAsync(ip, new Command(10, current));
      return true;
    }
  }
}
