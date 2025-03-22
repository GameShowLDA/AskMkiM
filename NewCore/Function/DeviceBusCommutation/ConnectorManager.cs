using NewCore.Base.Function.DBC;
using NewCore.Communication;
using static NewCore.Enum.DeviceEnum;
using static Utilities.LoggerUtility;

namespace NewCore.Function.DeviceBusCommutation
{
  /// <summary>
  /// Менеджер управления коммутацией устройств на шинах.
  /// </summary>
  public class ConnectorManager : IConnectorDeviceBusCommutation
  {
    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BusManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public ConnectorManager(Device.DeviceBusCommutation deviceBusCommutation) => _deviceBusCommutation = deviceBusCommutation;

    #region Мультиметр.

    /// <inheritdoc />
    public async Task<bool> ConnectMultimeter(SwitchingBusNew bus) => await SetMultimeterState(true, bus);

    /// <inheritdoc />
    public async Task<bool> DisconnectMultimeter(SwitchingBusNew bus) => await SetMultimeterState(false, bus);

    /// <summary>
    /// Устанавливает состояние мультиметра (подключение или отключение).
    /// </summary>
    /// <param name="connect">Флаг состояния: <c>true</c> – подключить, <c>false</c> – отключить.</param>
    /// <param name="bus">Шина, к которой подключается мультиметр.</param>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    private async Task<bool> SetMultimeterState(bool connect, SwitchingBusNew bus)
    {
      int numberConnector = (int)TypeConnector.Multimeter;
      if (TryGetBusNumber(bus, out int busNumber) && (busNumber < 1 || busNumber > 4))
      {
        var command = new DeviceCommand(5, numberConnector, busNumber, connect ? 1 : 2);
        await _deviceBusCommutation.DeviceProtocol.QueryAsync(command.ToString());
        await Task.Delay(10);
        return true;
      }

      LogError("Ошибка номера шины УКШ!");
      return false;
    }

    #endregion

    #region АЦП

    /// <inheritdoc />
    public async Task<bool> ConnectADC(SwitchingBusNew bus, bool reversePolarity = false) => await SetADCState(false, bus, reversePolarity);

    /// <inheritdoc />
    public async Task<bool> DisconnectADC(SwitchingBusNew bus, bool reversePolarity = false) => await SetADCState(false, bus, reversePolarity);

    /// <summary>
    /// Устанавливает состояние АЦП (подключение или отключение).
    /// </summary>
    /// <param name="connect">Флаг состояния: <c>true</c> – подключить, <c>false</c> – отключить.</param>
    /// <param name="bus">Шина, к которой подключается мультиметр.</param>
    /// <param name="reversePolarity">Флаг полюса: <c>true</c> – с переполюсовкой, <c>false</c> – без переполюсовки. </param>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    private async Task<bool> SetADCState(bool connect, SwitchingBusNew bus, bool reversePolarity)
    {
      int numberConnector = (int)TypeConnector.ADC;
      if (reversePolarity)
      {
        numberConnector++;
      }

      if (TryGetBusNumber(bus, out int busNumber) && (busNumber < 1 || busNumber > 4))
      {
        var command = new DeviceCommand(5, numberConnector, busNumber, connect ? 1 : 2);
        await _deviceBusCommutation.DeviceProtocol.QueryAsync(command.ToString());
        await Task.Delay(10);
        return true;
      }

      LogError("Ошибка номера шины УКШ!");
      return false;
    }

    #endregion

    #region ПИНТ

    /// <inheritdoc />
    public async Task<bool> ConnectPINT(SwitchingBusNew bus) => await SetPINTState(true, bus);

    /// <inheritdoc />
    public async Task<bool> DisconnectPINT(SwitchingBusNew bus) => await SetPINTState(true, bus);

    /// <summary>
    /// Устанавливает состояние ПИНТ (подключение или отключение).
    /// </summary>
    /// <param name="connect">Флаг состояния: <c>true</c> – подключить, <c>false</c> – отключить.</param>
    /// <param name="bus">Шина, к которой подключается мультиметр.</param>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    private async Task<bool> SetPINTState(bool connect, SwitchingBusNew bus)
    {
      int numberConnector = (int)TypeConnector.PINT;
      if (TryGetBusNumber(bus, out int busNumber) && (busNumber < 2 || busNumber > 3))
      {
        var command = new DeviceCommand(5, numberConnector, busNumber, connect ? 1 : 2);
        await _deviceBusCommutation.DeviceProtocol.QueryAsync(command.ToString());
        await Task.Delay(10);
        return true;
      }

      LogError("Ошибка номера шины УКШ!");
      return false;
    }

    #endregion

    #region Пробойка.

    /// <inheritdoc />
    public async Task<bool> ConnectBreakdownTester() => await SeBreakdownTesterState(true);

    /// <inheritdoc />
    public async Task<bool> DisconnectBreakdownTester() => await SeBreakdownTesterState(false);

    /// <summary>
    /// Устанавливает состояние мультиметра (подключение или отключение).
    /// </summary>
    /// <param name="connect">Флаг состояния: <c>true</c> – подключить, <c>false</c> – отключить.</param>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    private async Task<bool> SeBreakdownTesterState(bool connect)
    {
      int numberConnector = (int)TypeConnector.BreakdownTester;

      var command = new DeviceCommand(5, numberConnector, 1, connect ? 1 : 2);
      await _deviceBusCommutation.DeviceProtocol.QueryAsync(command.ToString());
      await Task.Delay(10);
      return true;
    }

    #endregion

    /// <summary>
    /// Извлекает номер шины из её имени.
    /// </summary>
    /// <param name="bus">Тип шины.</param>
    /// <param name="busNumber">Выходной параметр, содержащий номер шины.</param>
    /// <returns><c>true</c>, если номер успешно получен; иначе <c>false</c>.</returns>
    private bool TryGetBusNumber(SwitchingBusNew bus, out int busNumber)
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
  }
}
