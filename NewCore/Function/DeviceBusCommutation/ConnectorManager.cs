using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using NewCore.Communication;
using static Ask.LogLib.LoggerUtility;

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
    public async Task<bool> ConnectMultimeter(SwitchingBusNew bus, IUserInteractionService? userMessageService = null) => await SetMultimeterState(true, bus);

    /// <inheritdoc />
    public async Task<bool> DisconnectMultimeter(SwitchingBusNew bus, IUserInteractionService? userMessageService = null) => await SetMultimeterState(false, bus);

    /// <summary>
    /// Устанавливает состояние мультиметра (подключение или отключение).
    /// </summary>
    /// <param name="connect">Флаг состояния: <c>true</c> – подключить, <c>false</c> – отключить.</param>
    /// <param name="bus">Шина, к которой подключается мультиметр.</param>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    private async Task<bool> SetMultimeterState(bool connect, SwitchingBusNew bus, IUserInteractionService? userMessageService = null)
    {
      int numberConnector = (int)SwitchingDeviceTypeConnector.Multimeter;
      if (TryGetBusNumber(bus, out int busNumber) && (busNumber >= 1 || busNumber <= 4))
      {
        if (await ExecutionConfig.GetIsIdleModeEnabled())
        {
          return true;
        }

        var command = new DeviceCommand(5, numberConnector, busNumber, connect ? 1 : 2);
        await _deviceBusCommutation.DeviceProtocol.QueryAsync(command.ToString());
        await Task.Delay(10);
        return true;
      }

      LogError("Ошибка номера шины УКШ!", isDeviceLog: true);
      return false;
    }

    #endregion

    #region АЦП

    /// <inheritdoc />
    public async Task<bool> ConnectADC(SwitchingBusNew bus, bool reversePolarity = false, IUserInteractionService? userMessageService = null) => await SetADCState(false, bus, reversePolarity);

    /// <inheritdoc />
    public async Task<bool> DisconnectADC(SwitchingBusNew bus, bool reversePolarity = false, IUserInteractionService? userMessageService = null) => await SetADCState(false, bus, reversePolarity);

    /// <summary>
    /// Устанавливает состояние АЦП (подключение или отключение).
    /// </summary>
    /// <param name="connect">Флаг состояния: <c>true</c> – подключить, <c>false</c> – отключить.</param>
    /// <param name="bus">Шина, к которой подключается мультиметр.</param>
    /// <param name="reversePolarity">Флаг полюса: <c>true</c> – с переполюсовкой, <c>false</c> – без переполюсовки. </param>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    private async Task<bool> SetADCState(bool connect, SwitchingBusNew bus, bool reversePolarity, IUserInteractionService? userMessageService = null)
    {
      int numberConnector = (int)SwitchingDeviceTypeConnector.ADC;
      if (reversePolarity)
      {
        numberConnector++;
      }

      if (TryGetBusNumber(bus, out int busNumber) && (busNumber < 1 || busNumber > 4))
      {
        if (await ExecutionConfig.GetIsIdleModeEnabled())
        {
          return true;
        }

        var command = new DeviceCommand(5, numberConnector, busNumber, connect ? 1 : 2);
        await _deviceBusCommutation.DeviceProtocol.QueryAsync(command.ToString());
        await Task.Delay(10);
        return true;
      }

      LogError("Ошибка номера шины УКШ!", isDeviceLog: true);
      return false;
    }

    #endregion

    #region ПИНТ

    /// <inheritdoc />
    public async Task<bool> ConnectPINT(SwitchingBusNew bus, IUserInteractionService? userMessageService = null) => await SetPINTState(true, bus);

    /// <inheritdoc />
    public async Task<bool> DisconnectPINT(SwitchingBusNew bus, IUserInteractionService? userMessageService = null) => await SetPINTState(true, bus);

    /// <summary>
    /// Устанавливает состояние ПИНТ (подключение или отключение).
    /// </summary>
    /// <param name="connect">Флаг состояния: <c>true</c> – подключить, <c>false</c> – отключить.</param>
    /// <param name="bus">Шина, к которой подключается мультиметр.</param>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    private async Task<bool> SetPINTState(bool connect, SwitchingBusNew bus, IUserInteractionService? userMessageService = null)
    {
      int numberConnector = (int)SwitchingDeviceTypeConnector.PINT;
      if (TryGetBusNumber(bus, out int busNumber) && (busNumber < 2 || busNumber > 3))
      {
        if (await ExecutionConfig.GetIsIdleModeEnabled())
        {
          return true;
        }

        var command = new DeviceCommand(5, numberConnector, busNumber, connect ? 1 : 2);
        await _deviceBusCommutation.DeviceProtocol.QueryAsync(command.ToString());
        await Task.Delay(10);
        return true;
      }

      LogError("Ошибка номера шины УКШ!", isDeviceLog: true);
      return false;
    }

    #endregion

    #region Пробойка.

    /// <inheritdoc />
    public async Task<bool> ConnectBreakdownTester(IUserInteractionService? userMessageService = null) => await SetBreakdownTesterState(true);

    /// <inheritdoc />
    public async Task<bool> DisconnectBreakdownTester(IUserInteractionService? userMessageService = null) => await SetBreakdownTesterState(false);

    /// <summary>
    /// Устанавливает состояние мультиметра (подключение или отключение).
    /// </summary>
    /// <param name="connect">Флаг состояния: <c>true</c> – подключить, <c>false</c> – отключить.</param>
    /// <returns>Возвращает <c>true</c>, если операция выполнена успешно, иначе <c>false</c>.</returns>
    private async Task<bool> SetBreakdownTesterState(bool connect, IUserInteractionService? userMessageService = null)
    {
      int numberConnector = (int)SwitchingDeviceTypeConnector.BreakdownTester;

      if (await ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      var command = new DeviceCommand(5, numberConnector, 1, connect ? 1 : 2);
      var answer = await _deviceBusCommutation.DeviceProtocol.QueryAsync(command.ToString(), timeout: 1000);
      return answer.Contains("5.7.1.1");
    }

    #endregion

    #region Шины.

    /// <inheritdoc />
    public async Task<bool> ConnectAllBuses(IUserInteractionService? userMessageService = null)
    {
      return await SetAllBusesStatus(true);
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAllBuses(IUserInteractionService? userMessageService = null)
    {
      return await SetAllBusesStatus(false);
    }

    /// <summary>
    /// Устанавливает состояние всех шин на устройстве.
    /// </summary>
    /// <param name="connect"></param>
    /// <returns></returns>
    private async Task<bool> SetAllBusesStatus(bool connect, IUserInteractionService? userMessageService = null)
    {
      if (await ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      var command = new DeviceCommand(7, connect ? 1 : 2);
      var answer = await _deviceBusCommutation.DeviceProtocol.QueryAsync(command.ToString(), timeout: 1000);
      return connect ? answer.Contains("7.1") : answer.Contains("7.2");
    }

    #endregion

    /// <summary>
    /// Извлекает номер шины из её имени.
    /// </summary>
    /// <param name="bus">Тип шины.</param>
    /// <param name="busNumber">Выходной параметр, содержащий номер шины.</param>
    /// <returns><c>true</c>, если номер успешно получен; иначе <c>false</c>.</returns>
    private bool TryGetBusNumber(SwitchingBusNew bus, out int busNumber, IUserInteractionService? userMessageService = null)
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

    public async Task<bool> GetSuccesCurrentMode(SwitchingDeviceTypeConnector mode, IUserInteractionService? userMessageService = null)
    {
      var command = new DeviceCommand(51);
      var answer = await _deviceBusCommutation.DeviceProtocol.QueryAsync(command.ToString(), timeout: 1000);
      var expectingResult = $"51.{(int)mode}";
      return answer.Contains(expectingResult);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectBreakdownTesterAndMultimeter(IUserInteractionService? userMessageService = null)
    {
      var command = new DeviceCommand(5, 7, 0, 1);
      var answer = await _deviceBusCommutation.DeviceProtocol.QueryAsync(command.ToString(), timeout: 1000);
      var expectingResult = command.ToString();
      return answer.Contains(expectingResult);
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectBreakdownTesterAndMultimeter(IUserInteractionService? userMessageService = null)
    {
      var command = new DeviceCommand(5, 7, 0, 2);
      var answer = await _deviceBusCommutation.DeviceProtocol.QueryAsync(command.ToString(), timeout: 1000);
      var expectingResult = command.ToString();
      return answer.Contains(expectingResult);
    }
  }
}
