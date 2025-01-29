using System.Diagnostics;
using System.Net;
using Core.Communication;
using static Core.ModuleRelayControl.Enums;
using static Utilities.LoggerUtility;

namespace Core.ModuleRelayControl
{
  /// <summary>
  /// Функции модуля коммутации реле.
  /// </summary>
  public static class Functions
  {
    #region Измеритель.

    /// <summary>
    /// Включает измеритель модуля МКР.
    /// </summary>
    /// <param name="ip">IP-адрес модуля МКР.</param>
    /// <returns>Возвращает true, если команда отправлена успешно.</returns>
    /// <remarks>
    /// Этот метод формирует и отправляет команду на включение измерителя модуля МКР по указанному IP-адресу.
    /// </remarks>
    public static async Task<bool> ConnectMeterAsync(IPAddress ip)
    {
      Command cmd = new Command(5, 1);
      await CommunicationManager.SendCommandAsync(ip, cmd);
      return true;
    }

    /// <summary>
    /// Отключает измеритель модуля МКР.
    /// </summary>
    /// <param name="ip">IP-адрес модуля МКР.</param>
    /// <returns>Возвращает true, если команда отправлена успешно.</returns>
    /// <remarks>
    /// Этот метод формирует и отправляет команду на отключение измерителя модуля МКР по указанному IP-адресу.
    /// </remarks>
    public static async Task<bool> DisconnectMeterAsync(IPAddress ip)
    {
      Command cmd = new Command(5, 2);
      await CommunicationManager.SendCommandAsync(ip, cmd);
      return true;
    }

    /// <summary>
    /// Получить ответ от измерителя о замыкании шин или точек.
    /// </summary>
    /// <param name="ip">IP модуля коммутации реле.</param>
    /// <returns>true если есть замыкание, false если нет.</returns>
    public static async Task<bool> GetMeterResponseAsync(IPAddress ip)
    {
      Command cmd = new Command(7);
      return (await CommunicationManager.SendCommandAsync(ip, cmd, 1000)).Contains("105.1");
    }

    #endregion

    #region Работа с шинами.

    /// <summary>
    /// Подключить шину МКР.
    /// </summary>
    /// <param name="ip">Ip устройства.</param>
    /// <param name="bus">Замыкаемая шина.</param>
    /// <param name="lowVoltage">true - низковольтная шина, false - высоковольтная.</param>
    /// <returns>Результат замыкания шины.</returns>
    public static async Task<bool> ConnectBusAsync(IPAddress ip, BusModuleRelayControl bus, bool lowVoltage)
    {
      if (!TryGetBusNumber(bus, out int numberBus) || !TryGetBusType(bus, out int typeBus))
      {
        LogError("Ошибка данных шины!");
        return false;
      }

      int typeVoltage = lowVoltage ? numberBus : numberBus + 10;
      Command cmd = new Command(4, typeBus, typeVoltage, 1);

      LogInformation($"Команда: \"{cmd.ToString()}\". Замыкаем {(lowVoltage ? "низковольтную" : "высоковольтную")} шину {bus}.");
      await CommunicationManager.SendCommandAsync(ip, cmd, 1000);
      return true;
    }

    /// <summary>
    /// Отключение шин МКР.
    /// </summary>
    /// <param name="ip">Ip устройства.</param>
    /// <param name="bus">Замыкаемая шина.</param>
    /// <param name="lowVoltage">true - низковольтная шина, false - высоковольтная.</param>
    /// <returns>Результат замыкания шины.</returns>
    public static async Task<bool> DisconnectBusAsync(IPAddress ip, BusModuleRelayControl bus, bool lowVoltage)
    {
      if (!TryGetBusNumber(bus, out int numberBus) || !TryGetBusType(bus, out int typeBus))
      {
        LogError("Ошибка данных шины!");
        return false;
      }

      int typeVoltage = lowVoltage ? numberBus : numberBus + 10;
      Command cmd = new Command(4, typeBus, typeVoltage, 2);

      LogInformation($"Команда: \"{cmd.ToString()}\". Размыкаем {(lowVoltage ? "низковольтную" : "высоковольтную")} шину {bus}.");
      await Communication.CommunicationManager.SendCommandAsync(ip, new Command(4, typeBus, typeVoltage, 2), 1000);
      return true;
    }

    #endregion

    #region Работа с точками(реле).

    /// <summary>
    /// Подключить точку(реле) МКР.
    /// </summary>
    /// <param name="ip">IP МКР.</param>
    /// <param name="bus">Шина подключения.</param>
    /// <param name="number">Номер точки(реле).</param>
    /// <returns> Возвращает объект типа Task.</returns>
    public static async Task<bool> ConnectRelayAsync(IPAddress ip, BusPoint bus, int number)
    {
      await CommunicationManager.SendCommandAsync(ip, new Command(8, number, (int)bus, 1));
      await Task.Delay(5);
      return true;
    }

    /// <summary>
    /// Отключить точку(реле) МКР.
    /// </summary>
    /// <param name="ip">IP МКР.</param>
    /// <param name="bus">Шина подключения.</param>
    /// <param name="number">Номер точки(реле).</param>
    /// <returns> Возвращает объект типа Task.</returns>
    public static async Task<bool> DisconnectRelayAsync(IPAddress ip, BusPoint bus, int number)
    {
      await CommunicationManager.SendCommandAsync(ip, new Command(8, number, (int)bus, 2));
      await Task.Delay(5);
      return true;
    }

    /// <summary>
    /// Подключение диапазона точек МКР.
    /// </summary>
    /// <param name="ip">IP МКР.</param>
    /// <param name="bus">Подключаемая шина.</param>
    /// <param name="firtsPoint">Первая тоска в диапазоне</param>
    /// <param name="lastPoint">Последняя точка в диапазоне.</param>
    /// <returns>Результат подключения.</returns>
    public static async Task<bool> ConnectRelayGroupAsync(IPAddress ip, BusPoint bus, int firtsPoint, int lastPoint)
    {
      Command command = new Command();
      command.Number = 11;
      command.FirstParameter = firtsPoint;
      command.SecondParameter = lastPoint;
      command.ThirdParameter = (1 * 10) + (int)bus;

      Stopwatch stopwatch = new Stopwatch();
      stopwatch.Start();
      string answer = await CommunicationManager.SendCommandAsync(ip, command, 3000);
      stopwatch.Stop();
      Console.WriteLine($"Время ожидания: {stopwatch.Elapsed}");
      if (answer.Contains("11.1"))
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    /// <summary>
    /// Отключение диапазона точек МКР.
    /// </summary>
    /// <param name="ip">IP МКР.</param>
    /// <param name="bus">Подключаемая шина.</param>
    /// <param name="firtsPoint">Первая тоска в диапазоне</param>
    /// <param name="lastPoint">Последняя точка в диапазоне.</param>
    /// <returns>Результат подключения.</returns>
    public static async Task<bool> DisconnectRelayGroupAsync(IPAddress ip, BusPoint bus, int firtsPoint, int lastPoint)
    {
      Command command = new Command();
      command.Number = 11;
      command.FirstParameter = firtsPoint;
      command.SecondParameter = lastPoint;
      command.ThirdParameter = (2 * 10) + (int)bus;
      await CommunicationManager.SendCommandAsync(ip, command);
      await Task.Delay(5);
      return true;
    }

    #endregion

    #region Самоконтроль.

    /// <summary>
    /// Проверяет точку на работоспособность у МКР.
    /// </summary>
    /// <param name="ip">ip МКР.</param>
    /// <param name="numberPoint">Номер точки.</param>
    /// <returns>Возвращает ответ от устрйоства.</returns>
    public static async Task<string> CheckPoint(IPAddress ip, int numberPoint)
    {
      return await CommunicationManager.SendCommandAsync(ip, new Command(6, numberPoint), 1000);
    }

    #endregion

    #region Дополнительное.

    /// <summary>
    /// Инициализация модуля коммутации реле.
    /// </summary>
    /// <param name="ip">ip МКР.</param>
    /// <returns>Возвращает ответ, получен ли ответ от инициализации.</returns>
    public static async Task<bool> Initialize(IPAddress ip)
    {
      return (await CommunicationManager.SendCommandAsync(ip, new Command(1), 1000)) == "1.0.350";
    }

    /// <summary>
    /// Выполняет сброс всех реле на МКР.
    /// </summary>
    /// <param name="ip">IP адресc.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public static async Task ResetAsync(IPAddress ip)
    {
      await CommunicationManager.SendCommandAsync(ip, new Command(2));
    }

    #endregion

    /// <summary>
    /// Пытается получить номер шины на основе значения перечисления BusModuleRelayControl.
    /// </summary>
    /// <param name="bus">Значение перечисления BusModuleRelayControl.</param>
    /// <param name="busNumber">Выходной параметр, который содержит номер шины, если операция успешна.</param>
    /// <returns>Возвращает true, если номер шины успешно получен; в противном случае - false.</returns>
    private static bool TryGetBusNumber(BusModuleRelayControl bus, out int busNumber)
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
    /// Пытается преобразовать шину в тип (A, B, AB) на основе значения перечисления BusModuleRelayControl.
    /// </summary>
    /// <param name="bus">Значение перечисления BusModuleRelayControl.</param>
    /// <param name="busType">Выходной параметр, который содержит тип шины (1 для A, 2 для B, 3 для AB), если операция успешна.</param>
    /// <returns>Возвращает true, если тип шины успешно получен; в противном случае - false.</returns>
    private static bool TryGetBusType(BusModuleRelayControl bus, out int busType)
    {
      busType = -1;

      if (bus is BusModuleRelayControl.A1 || bus is BusModuleRelayControl.A2 || bus is BusModuleRelayControl.A3 || bus is BusModuleRelayControl.A4)
      {
        busType = 1;
      }
      else if (bus is BusModuleRelayControl.B1 || bus is BusModuleRelayControl.B2 || bus is BusModuleRelayControl.B3 || bus is BusModuleRelayControl.B4)
      {
        busType = 2;
      }
      else if (bus is BusModuleRelayControl.AB1 || bus is BusModuleRelayControl.AB2 || bus is BusModuleRelayControl.AB3 || bus is BusModuleRelayControl.AB4)
      {
        busType = 3;
      }

      return busType != -1;
    }
  }
}
