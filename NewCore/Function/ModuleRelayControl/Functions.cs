using NewCore.Communication;
using System.Diagnostics;
using System.Net;
using static NewCore.Enum.DeviceEnum;
using static Utilities.LoggerUtility;


namespace NewCore.Function.ModuleRelayControl
{
  /// <summary>
  /// Функции модуля коммутации реле.
  /// </summary>
  public class Functions
  {
    public Functions(Device.ModuleRelayControl moduleRelayControl) => _moduleRelayControl = moduleRelayControl;
    Device.ModuleRelayControl _moduleRelayControl { get; set; }

    #region Измеритель.

    /// <summary>
    /// Включает измеритель модуля МКР.
    /// </summary>
    /// <returns>Возвращает true, если команда отправлена успешно.</returns>
    /// <remarks>
    /// Этот метод формирует и отправляет команду на включение измерителя модуля МКР по указанному IP-адресу.
    /// </remarks>
    public async Task<bool> ConnectMeterAsync()
    {
      DeviceCommand cmd = new DeviceCommand(5, 1);
      await DeviceCommandSender.SendCommandAsync(_moduleRelayControl.IPAddress, cmd);
      return true;
    }

    /// <summary>
    /// Отключает измеритель модуля МКР.
    /// </summary>
    /// <returns>Возвращает true, если команда отправлена успешно.</returns>
    /// <remarks>
    /// Этот метод формирует и отправляет команду на отключение измерителя модуля МКР по указанному IP-адресу.
    /// </remarks>
    public async Task<bool> DisconnectMeterAsync()
    {
      DeviceCommand cmd = new DeviceCommand(5, 2);
      await DeviceCommandSender.SendCommandAsync(_moduleRelayControl.IPAddress, cmd);
      return true;
    }

    /// <summary>
    /// Получить ответ от измерителя о замыкании шин или точек.
    /// </summary>
    /// <returns>true если есть замыкание, false если нет.</returns>
    public async Task<bool> GetMeterResponseAsync()
    {
      DeviceCommand cmd = new DeviceCommand(7);
      return (await DeviceCommandSender.SendCommandAsync(_moduleRelayControl.IPAddress, cmd, 1000)).Contains("105.1");
    }

    #endregion

    #region Работа с шинами.

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
      await DeviceCommandSender.SendCommandAsync(_moduleRelayControl.IPAddress, cmd, 1000);
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
      await Communication.DeviceCommandSender.SendCommandAsync(_moduleRelayControl.IPAddress, new DeviceCommand(4, typeBus, typeVoltage, 2), 1000);
      return true;
    }

    #endregion

    #region Работа с точками(реле).

    /// <summary>
    /// Подключить точку(реле) МКР.
    /// </summary>
    /// <param name="bus">Шина подключения.</param>
    /// <param name="number">Номер точки(реле).</param>
    /// <returns> Возвращает объект типа Task.</returns>
    public async Task<bool> ConnectRelayAsync(BusPoint bus, int number)
    {
      await DeviceCommandSender.SendCommandAsync(_moduleRelayControl.IPAddress, new DeviceCommand(8, number, (int)bus, 1));
      await Task.Delay(5);
      return true;
    }

    /// <summary>
    /// Отключить точку(реле) МКР.
    /// </summary>
    /// <param name="_moduleRelayControl.IPAddress">IP МКР.</param>
    /// <param name="bus">Шина подключения.</param>
    /// <param name="number">Номер точки(реле).</param>
    /// <returns> Возвращает объект типа Task.</returns>
    public async Task<bool> DisconnectRelayAsync(BusPoint bus, int number)
    {
      await DeviceCommandSender.SendCommandAsync(_moduleRelayControl.IPAddress, new DeviceCommand(8, number, (int)bus, 2));
      await Task.Delay(5);
      return true;
    }

    /// <summary>
    /// Подключение диапазона точек МКР.
    /// </summary>
    /// <param name="bus">Подключаемая шина.</param>
    /// <param name="firtsPoint">Первая тоска в диапазоне</param>
    /// <param name="lastPoint">Последняя точка в диапазоне.</param>
    /// <returns>Результат подключения.</returns>
    public async Task<bool> ConnectRelayGroupAsync(BusPoint bus, int firtsPoint, int lastPoint)
    {
      DeviceCommand command = new DeviceCommand();
      command.Number = 11;
      command.FirstParameter = firtsPoint;
      command.SecondParameter = lastPoint;
      command.ThirdParameter = (1 * 10) + (int)bus;

      Stopwatch stopwatch = new Stopwatch();
      stopwatch.Start();
      string answer = await DeviceCommandSender.SendCommandAsync(_moduleRelayControl.IPAddress, command, 3000);
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
    /// <param name="bus">Подключаемая шина.</param>
    /// <param name="firtsPoint">Первая тоска в диапазоне</param>
    /// <param name="lastPoint">Последняя точка в диапазоне.</param>
    /// <returns>Результат подключения.</returns>
    public async Task<bool> DisconnectRelayGroupAsync(BusPoint bus, int firtsPoint, int lastPoint)
    {
      DeviceCommand command = new DeviceCommand();
      command.Number = 11;
      command.FirstParameter = firtsPoint;
      command.SecondParameter = lastPoint;
      command.ThirdParameter = (2 * 10) + (int)bus;
      await DeviceCommandSender.SendCommandAsync(_moduleRelayControl.IPAddress, command);
      await Task.Delay(5);
      return true;
    }

    #endregion

    #region Самоконтроль.

    /// <summary>
    /// Проверяет точку на работоспособность у МКР.
    /// </summary>
    /// <param name="numberPoint">Номер точки.</param>
    /// <returns>Возвращает ответ от устрйоства.</returns>
    public async Task<string> CheckPoint(int numberPoint)
    {
      return await DeviceCommandSender.SendCommandAsync(_moduleRelayControl.IPAddress, new DeviceCommand(6, numberPoint), 1000);
    }

    #endregion

    #region Дополнительное.

    /// <summary>
    /// Инициализация модуля коммутации реле.
    /// </summary>
    /// <returns>Возвращает ответ, получен ли ответ от инициализации.</returns>
    public async Task<(bool Connect, string Answer)> Initialize()
    {
      DeviceCommand cmd = new DeviceCommand(1, 0, 0, 0);
      string result = await DeviceCommandSender.SendCommandAsync(_moduleRelayControl.IPAddress, cmd, 2000).ConfigureAwait(true);
      return result == "1.0.1" ? (true, string.Empty) : (false, result);
    }

    /// <summary>
    /// Выполняет сброс всех реле на МКР.
    /// </summary>
    /// <param name="_moduleRelayControl.IPAddress">IP адресc.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task ResetAsync()
    {
      await DeviceCommandSender.SendCommandAsync(_moduleRelayControl.IPAddress, new DeviceCommand(2));
    }

    #endregion

    /// <summary>
    /// Пытается получить номер шины на основе значения перечисления SwitchingBus.
    /// </summary>
    /// <param name="bus">Значение перечисления SwitchingBus.</param>
    /// <param name="busNumber">Выходной параметр, который содержит номер шины, если операция успешна.</param>
    /// <returns>Возвращает true, если номер шины успешно получен; в противном случае - false.</returns>
    private bool TryGetBusNumber(SwitchingBus bus, out int busNumber)
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
    private bool TryGetBusType(SwitchingBus bus, out int busType)
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
