using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.Commands;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.Function.DeviceBusCommutation
{
  /// <summary>
  /// Менеджер управления коммутацией устройств на шинах.
  /// </summary>
  public class ConnectorManager : IConnectorDeviceBusCommutation
  {
    /// <summary>
    /// Состояние подключений устройств к шинам.
    /// </summary>
    private readonly DeviceBusConnectionStateStore connectionState = new DeviceBusConnectionStateStore();

    /// <summary>
    /// Шина, используемая для регистрации подключения пробойной установки.
    /// </summary>
    private const SwitchingBusNew BreakdownBus = SwitchingBusNew.AB1;

    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;
    private readonly DeviceBusCommutationQueryExecutor queryExecutor;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ConnectorManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Устройство коммутации шин.</param>
    public ConnectorManager(Device.DeviceBusCommutation deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation;
      queryExecutor = new DeviceBusCommutationQueryExecutor(deviceBusCommutation);
      _deviceBusCommutation.ConnectableManager.IsReset += ConnectableManager_IsReset;
      ConnectableManager_IsReset();
    }

    /// <summary>
    /// Сбрасывает сохранённые состояния подключений после сброса устройства.
    /// </summary>
    private void ConnectableManager_IsReset()
    {
      connectionState.Reset();
    }

    #region Мультиметр.

    /// <inheritdoc />
    public async Task<bool> ConnectMultimeter(SwitchingBusNew bus, IUserInteractionService? userMessageService = null)
    {
      foreach (var connected in connectionState.GetConnected(DeviceBusConnectionType.Multimeter))
      {
        var disconnectResult = await DisconnectMultimeter(connected.Bus);
        connectionState.Set(DeviceBusConnectionType.Multimeter, connected.Bus, false);
      }

      var result = await SetMultimeterState(true, bus);

      if (result)
      {
        connectionState.Set(DeviceBusConnectionType.Multimeter, bus, true);
      }
      else
      {
        connectionState.Set(DeviceBusConnectionType.Multimeter, bus, false);
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectMultimeter(SwitchingBusNew bus, IUserInteractionService? userMessageService = null)
    {
      var result = await SetMultimeterState(false, bus);
      connectionState.Set(DeviceBusConnectionType.Multimeter, bus, false);
      return result;
    }

    /// <summary>
    /// Подключает мультиметр к указанной шине или отключает его от неё.
    /// </summary>
    /// <param name="connect">
    /// <see langword="true"/>, чтобы подключить мультиметр; <see langword="false"/>, чтобы отключить.
    /// </param>
    /// <param name="bus">Коммутируемая шина.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>
    /// <see langword="true"/>, если состояние мультиметра изменено успешно; иначе — <see langword="false"/>.
    /// </returns>
    private async Task<bool> SetMultimeterState(bool connect, SwitchingBusNew bus, IUserInteractionService? userMessageService = null)
    {
      int numberConnector = (int)SwitchingDeviceTypeConnector.Multimeter;
      if (TryGetBusNumber(bus, out int busNumber) && busNumber >= 1 && busNumber <= 4)
      {
        var command = new DeviceCommand(5, numberConnector, busNumber, connect ? 1 : 2);
        var answer = await queryExecutor.QueryAsync(command.ToString());
        await Task.Delay(10);
        var expectingResult = (command.ToString()).Substring(0, command.ToString().Length - 1);
        return !string.IsNullOrWhiteSpace(answer) && answer.Contains(expectingResult);
      }

      LogError("Ошибка номера шины УКШ!", isDeviceLog: true);
      return false;
    }

    #endregion

    #region АЦП

    /// <summary>
    /// Подключает АЦП к указанной шине.
    /// </summary>
    /// <param name="bus">Шина, к которой подключается АЦП.</param>
    /// <param name="reversePolarity">
    /// <see langword="true"/>, чтобы подключить АЦП с обратной полярностью; иначе — <see langword="false"/>.
    /// </param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>
    /// <see langword="true"/>, если АЦП подключён успешно; иначе — <see langword="false"/>.
    /// </returns>
    public async Task<bool> ConnectADC(SwitchingBusNew bus, bool reversePolarity = false, IUserInteractionService? userMessageService = null) => await SetADCState(true, bus, reversePolarity);

    /// <summary>
    /// Отключает АЦП от указанной шины.
    /// </summary>
    /// <param name="bus">Шина, от которой отключается АЦП.</param>
    /// <param name="reversePolarity">
    /// <see langword="true"/>, если АЦП подключён с обратной полярностью; иначе — <see langword="false"/>.
    /// </param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>
    /// <see langword="true"/>, если АЦП отключён успешно; иначе — <see langword="false"/>.
    /// </returns>
    public async Task<bool> DisconnectADC(SwitchingBusNew bus, bool reversePolarity = false, IUserInteractionService? userMessageService = null) => await SetADCState(false, bus, reversePolarity);

    /// <summary>
    /// Подключает АЦП к указанной шине или отключает его от неё.
    /// </summary>
    /// <param name="connect">
    /// <see langword="true"/>, чтобы подключить АЦП; <see langword="false"/>, чтобы отключить.
    /// </param>
    /// <param name="bus">Коммутируемая шина.</param>
    /// <param name="reversePolarity">
    /// <see langword="true"/>, чтобы использовать обратную полярность; иначе — <see langword="false"/>.
    /// </param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>
    /// <see langword="true"/>, если состояние АЦП изменено успешно; иначе — <see langword="false"/>.
    /// </returns>
    /// <exception cref="Exception">Операция временно отключена.</exception>
    private async Task<bool> SetADCState(bool connect, SwitchingBusNew bus, bool reversePolarity, IUserInteractionService? userMessageService = null)
    {
      throw new Exception("Временно откличли в Ask.Device.Runtime.Function.DeviceBusCommutation.ConnectorManager.SetADCState");
      //int numberConnector = (int)SwitchingDeviceTypeConnector.ADC;
      //if (reversePolarity)
      //{
      //  numberConnector++;
      //}

      //if (TryGetBusNumber(bus, out int busNumber) && busNumber >= 1 && busNumber <= 4)
      //{
      //  if (ExecutionConfig.GetIsIdleModeEnabled())
      //  {
      //    return !IdleHardwareErrorSimulator.ShouldSimulateHardwareError();
      //  }

      //  var command = new DeviceCommand(5, numberConnector, busNumber, connect ? 1 : 2);
      //  var answer = await _deviceBusCommutation.DeviceProtocol.QueryAsync(command.ToString(), timeout: 1000);
      //  await Task.Delay(10);
      //  return !string.IsNullOrWhiteSpace(answer) && answer.Contains(command.ToString());
      //}

      //LogError("Ошибка номера шины УКШ!", isDeviceLog: true);
      //return false;
    }

    #endregion

    #region ПИНТ

    /// <inheritdoc />
    public async Task<bool> ConnectPINT(SwitchingBusNew bus, IUserInteractionService? userMessageService = null)
    {
      foreach (var connected in connectionState.GetConnected(DeviceBusConnectionType.PINT))
      {
        var disconnectResult = await DisconnectPINT(connected.Bus);
        connectionState.Set(DeviceBusConnectionType.PINT, connected.Bus, false);
      }

      var result = await SetPINTState(true, bus);
      if (result)
      {
        connectionState.Set(DeviceBusConnectionType.PINT, bus, true);
      }
      else
      {
        connectionState.Set(DeviceBusConnectionType.PINT, bus, false);
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectPINT(SwitchingBusNew bus, IUserInteractionService? userMessageService = null)
    {
      var result = await SetPINTState(false, bus);
      connectionState.Set(DeviceBusConnectionType.PINT, bus, false);
      return result;
    }

    /// <summary>
    /// Подключает ПИНТ к указанной шине или отключает его от неё.
    /// </summary>
    /// <param name="connect">
    /// <see langword="true"/>, чтобы подключить ПИНТ; <see langword="false"/>, чтобы отключить.
    /// </param>
    /// <param name="bus">Коммутируемая шина.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>
    /// <see langword="true"/>, если состояние ПИНТ изменено успешно; иначе — <see langword="false"/>.
    /// </returns>
    /// <exception cref="Exception">Операция временно отключена.</exception>
    private async Task<bool> SetPINTState(bool connect, SwitchingBusNew bus, IUserInteractionService? userMessageService = null)
    {
      throw new Exception("Временно откличли в Ask.Device.Runtime.Function.DeviceBusCommutation.ConnectorManager.SetPINTState");
      //int numberConnector = (int)SwitchingDeviceTypeConnector.PINT;
      //if (TryGetBusNumber(bus, out int busNumber) && busNumber >= 2 && busNumber <= 3)
      //{
      //  if (ExecutionConfig.GetIsIdleModeEnabled())
      //  {
      //    return !IdleHardwareErrorSimulator.ShouldSimulateHardwareError();
      //  }

      //  var command = new DeviceCommand(5, numberConnector, busNumber, connect ? 1 : 2);
      //  var answer = await _deviceBusCommutation.DeviceProtocol.QueryAsync(command.ToString(), timeout: 1000);
      //  await Task.Delay(10);
      //  return !string.IsNullOrWhiteSpace(answer) && answer.Contains(command.ToString());
      //}

      //LogError("Ошибка номера шины УКШ!", isDeviceLog: true);
      //return false;
    }

    #endregion

    #region Пробойка.

    /// <inheritdoc />
    public async Task<bool> ConnectBreakdownTester(IUserInteractionService? userMessageService = null)
    {
      var result = await SetBreakdownTesterState(true);

      if (result)
      {
        connectionState.Set(DeviceBusConnectionType.BreakdownTester, BreakdownBus, true);
      }
      else
      {
        connectionState.Set(DeviceBusConnectionType.BreakdownTester, BreakdownBus, false);
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectBreakdownTester(IUserInteractionService? userMessageService = null)
    {
      var result = await SetBreakdownTesterState(false);

      connectionState.Set(DeviceBusConnectionType.BreakdownTester, BreakdownBus, false);
      return result;
    }

    /// <summary>
    /// Подключает пробойную установку или отключает её.
    /// </summary>
    /// <param name="connect">
    /// <see langword="true"/>, чтобы подключить пробойную установку; <see langword="false"/>, чтобы отключить.
    /// </param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>
    /// <see langword="true"/>, если состояние пробойной установки изменено успешно;
    /// иначе — <see langword="false"/>.
    /// </returns>
    private async Task<bool> SetBreakdownTesterState(bool connect, IUserInteractionService? userMessageService = null)
    {
      int numberConnector = (int)SwitchingDeviceTypeConnector.BreakdownTester;

      var command = new DeviceCommand(5, numberConnector, 1, connect ? 1 : 2);
      var answer = await queryExecutor.QueryAsync(command.ToString());
      var expectingResult = (command.ToString()).Substring(0, command.ToString().Length - 1);

      return !string.IsNullOrWhiteSpace(answer) && answer.Contains(expectingResult);
    }

    #endregion

    #region Делитель.

    /// <inheritdoc />
    public async Task<bool> EnableDivider(IUserInteractionService? userMessageService = null)
    {

      var command = new DeviceCommand(9, 2, 0, 1);
      var answer = await queryExecutor.QueryAsync(command.ToString());
      var expectingResult = (command.ToString()).Substring(0, command.ToString().Length - 1);

      return !string.IsNullOrWhiteSpace(answer) && answer.Contains(expectingResult);
    }

    /// <inheritdoc />
    public async Task<bool> DisableDivider(IUserInteractionService? userMessageService = null)
    {

      var command = new DeviceCommand(9, 2, 0, 2);
      var answer = await queryExecutor.QueryAsync(command.ToString());
      var expectingResult = (command.ToString()).Substring(0, command.ToString().Length - 1);

      return !string.IsNullOrWhiteSpace(answer) && answer.Contains(expectingResult);
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
    /// Подключает или отключает все шины устройства.
    /// </summary>
    /// <param name="connect">
    /// <see langword="true"/>, чтобы подключить все шины; <see langword="false"/>, чтобы отключить.
    /// </param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>
    /// <see langword="true"/>, если состояние всех шин изменено успешно; иначе — <see langword="false"/>.
    /// </returns>
    private async Task<bool> SetAllBusesStatus(bool connect, IUserInteractionService? userMessageService = null)
    {
      var command = new DeviceCommand(7, connect ? 1 : 2);
      var answer = await queryExecutor.QueryAsync(command.ToString());
      return !string.IsNullOrWhiteSpace(answer) && (connect ? answer.Contains("7.1") : answer.Contains("7.2"));
    }

    #endregion

    /// <summary>
    /// Извлекает номер шины из её имени.
    /// </summary>
    /// <param name="bus">Тип шины.</param>
    /// <param name="busNumber">Полученный номер шины.</param>
    /// <param name="userMessageService">Сервис взаимодействия с пользователем.</param>
    /// <returns>
    /// <see langword="true"/>, если номер шины получен успешно; иначе — <see langword="false"/>.
    /// </returns>
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

    /// <inheritdoc />
    public async Task<bool> ConnectBreakdownTesterAndMultimeter(IUserInteractionService? userMessageService = null)
    {
      var command = new DeviceCommand(5, 7, 0, 1);
      var answer = await queryExecutor.QueryAsync(command.ToString());
      var expectingResult = command.ToString();
      var result = !string.IsNullOrWhiteSpace(answer) && answer.Contains(expectingResult);

      if (result)
      {
        connectionState.Set(DeviceBusConnectionType.BreakdownTesterAndMultimeter, BreakdownBus, true);
      }
      else
      {
        connectionState.Set(DeviceBusConnectionType.BreakdownTesterAndMultimeter, BreakdownBus, false);
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectBreakdownTesterAndMultimeter(IUserInteractionService? userMessageService = null)
    {
      var command = new DeviceCommand(5, 7, 0, 2);
      var answer = await queryExecutor.QueryAsync(command.ToString());
      var expectingResult = command.ToString();
      var result = !string.IsNullOrWhiteSpace(answer) && answer.Contains(expectingResult);

      connectionState.Set(DeviceBusConnectionType.BreakdownTesterAndMultimeter, BreakdownBus, false);
      return result;
    }

    /// <summary>
    /// Возвращает сведения о подключённых устройствах.
    /// </summary>
    /// <returns>Список текущих подключений устройств к шинам.</returns>
    public IReadOnlyList<DeviceConnectionInfo> GetConnectedDevices()
    {
      return connectionState.GetConnectedDevices();
    }

  }
}
