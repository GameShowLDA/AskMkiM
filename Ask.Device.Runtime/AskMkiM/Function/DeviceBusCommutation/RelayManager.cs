using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.ResponseProcessor.DeviceBusCommutation.ResponseProcessing;
using Ask.Device.Runtime.AskMkiM.Base.Commands;

namespace Ask.Device.Runtime.AskMkiM.Function.DeviceBusCommutation
{
  /// <summary>
  /// Менеджер управления реле коммутации.
  /// Отвечает за подключение и отключение реле в системе.
  /// </summary>
  public class RelayManager : IRelayDeviceBusCommutation
  {
    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly Device.SwitchingDevice.DeviceBusCommutation _deviceBusCommutation;
    private readonly DeviceBusCommutationQueryExecutor queryExecutor;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BusManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public RelayManager(Device.SwitchingDevice.DeviceBusCommutation deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation;
      queryExecutor = new DeviceBusCommutationQueryExecutor(deviceBusCommutation);
    }

    /// <summary>
    /// Подключения реле.
    /// </summary>
    /// <param name="numberRelay">Номер реле, которое необходимо замкнуть.</param>
    /// <returns>Результат проверки и выполнения команды.</returns>
    public async Task<bool> ConnectRelay(int numberRelay, IUserInteractionService? userMessageService = null)
    {
      if (numberRelay < 0)
      {
        return false;
      }

      DeviceCommand cmd = new DeviceCommand(8, numberRelay, 1);
      string answer = await queryExecutor.QueryAsync(cmd.ToString());
      await Task.Delay(10);
      return await DeviceBusCommutationResponseProcessor.CheckRelayOperationAsync(
        answer, _deviceBusCommutation, numberRelay, true, userMessageService);
    }

    /// <summary>
    /// Подключение реле.
    /// </summary>
    /// <param name="numberRelay">Номер реле, которое необходимо замкнуть.</param>
    /// <returns>Результат проверки и выполнения команды.</returns>
    public async Task<bool> DisconnectRelay(int numberRelay, IUserInteractionService? userMessageService = null)
    {
      if (numberRelay < 0)
      {
        return false;
      }

      DeviceCommand cmd = new DeviceCommand(8, numberRelay, 2);
      string answer = await queryExecutor.QueryAsync(cmd.ToString());
      await Task.Delay(10);
      return await DeviceBusCommutationResponseProcessor.CheckRelayOperationAsync(
        answer, _deviceBusCommutation, numberRelay, false, userMessageService);
    }

    /// <summary>
    /// Включение реле.
    /// </summary>
    /// <returns>Результат проверки и выполнения команды.</returns>
    public async Task<bool> EnableRelay(IUserInteractionService? userMessageService = null)
    {
      var cmd = new DeviceCommand(9, 1, 0, 1);
      string answer = await queryExecutor.QueryAsync(cmd.ToString());

      await Task.Delay(10);
      return await DeviceBusCommutationResponseProcessor.CheckAccessoryOperationAsync(
        answer, _deviceBusCommutation, 1, 0, true, "Включение реле", "Общий", outputService: userMessageService);
    }

    /// <summary>
    /// Выключение реле.
    /// </summary>
    /// <returns>Результат проверки и выполнения команды.</returns>
    public async Task<bool> DisableRelay(IUserInteractionService? userMessageService = null)
    {
      var cmd = new DeviceCommand(9, 1, 0, 2);
      string answer = await queryExecutor.QueryAsync(cmd.ToString());

      await Task.Delay(10);
      return await DeviceBusCommutationResponseProcessor.CheckAccessoryOperationAsync(
        answer, _deviceBusCommutation, 1, 0, false, "Выключение реле", "Общий", outputService: userMessageService);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectRCRelay(IUserInteractionService? userMessageService = null)
    {

      DeviceCommand cmd = new DeviceCommand(9, 3, 0, 1);
      string answer = await queryExecutor.QueryAsync(cmd.ToString());
      await Task.Delay(10);
      return await DeviceBusCommutationResponseProcessor.CheckAccessoryOperationAsync(
        answer, _deviceBusCommutation, 3, 0, true, "Подключение RC реле", "Общий", outputService: userMessageService);
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectRCRelay(IUserInteractionService? userMessageService = null)
    {
      DeviceCommand cmd = new DeviceCommand(9, 3, 0, 2);
      string answer = await queryExecutor.QueryAsync(cmd.ToString());
      await Task.Delay(10);
      return await DeviceBusCommutationResponseProcessor.CheckAccessoryOperationAsync(
        answer, _deviceBusCommutation, 3, 0, false, "Отключение RC реле", "Общий", outputService: userMessageService);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectResistor(int numberResistor, IUserInteractionService? userMessageService = null)
    {
      if (numberResistor < 1 || numberResistor > 8)
      {
        return false;
      }

      DeviceCommand cmd = new DeviceCommand(9, 3, numberResistor, 1);
      string answer = await queryExecutor.QueryAsync(cmd.ToString());
      await Task.Delay(10);
      return await DeviceBusCommutationResponseProcessor.CheckAccessoryOperationAsync(
        answer, _deviceBusCommutation, 3, numberResistor, true,
        "Подключение резистора RC реле", $"R{numberResistor}", outputService: userMessageService);
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectResistor(int numberResistor, IUserInteractionService? userMessageService = null)
    {
      if (numberResistor < 1 || numberResistor > 8)
      {
        return false;
      }

      DeviceCommand cmd = new DeviceCommand(9, 3, numberResistor, 2);
      string answer = await queryExecutor.QueryAsync(cmd.ToString());
      await Task.Delay(10);
      return await DeviceBusCommutationResponseProcessor.CheckAccessoryOperationAsync(
        answer, _deviceBusCommutation, 3, numberResistor, false,
        "Отключение резистора RC реле", $"R{numberResistor}", outputService: userMessageService);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectCapacitor(int numberCapacitor, IUserInteractionService? userMessageService = null)
    {
      if (numberCapacitor < 1 || numberCapacitor > 6)
      {
        return false;
      }

      DeviceCommand cmd = new DeviceCommand(9, 3, numberCapacitor + 10, 1);
      string answer = await queryExecutor.QueryAsync(cmd.ToString());
      await Task.Delay(10);
      return await DeviceBusCommutationResponseProcessor.CheckAccessoryOperationAsync(
        answer, _deviceBusCommutation, 3, numberCapacitor + 10, true,
        "Подключение конденсатора RC реле", $"C{numberCapacitor}", outputService: userMessageService);
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectCapacitor(int numberCapacitor, IUserInteractionService? userMessageService = null)
    {
      if (numberCapacitor < 1 || numberCapacitor > 6)
      {
        return false;
      }

      DeviceCommand cmd = new DeviceCommand(9, 3, numberCapacitor + 10, 2);
      string answer = await queryExecutor.QueryAsync(cmd.ToString());
      await Task.Delay(10);
      return await DeviceBusCommutationResponseProcessor.CheckAccessoryOperationAsync(
        answer, _deviceBusCommutation, 3, numberCapacitor + 10, false,
        "Отключение конденсатора RC реле", $"C{numberCapacitor}", outputService: userMessageService);
    }
  }
}


