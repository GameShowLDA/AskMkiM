using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Commands;

namespace Ask.Device.Runtime.Function.DeviceBusCommutation
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
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;
    private readonly DeviceBusCommutationQueryExecutor queryExecutor;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BusManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public RelayManager(Device.DeviceBusCommutation deviceBusCommutation)
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
      return !ExecutionConfig.GetIsIdleModeEnabled() || !string.IsNullOrWhiteSpace(answer);
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
      return !ExecutionConfig.GetIsIdleModeEnabled() || !string.IsNullOrWhiteSpace(answer);
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
      return !string.IsNullOrWhiteSpace(answer) && answer.Contains(cmd.ToString());
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
      return !string.IsNullOrWhiteSpace(answer) && answer.Contains(cmd.ToString());
    }

    /// <inheritdoc />
    public async Task<bool> ConnectRCRelay(IUserInteractionService? userMessageService = null)
    {

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      DeviceCommand cmd = new DeviceCommand(9, 3, 0, 1);
      await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString());
      await Task.Delay(10);
      return true;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectRCRelay(IUserInteractionService? userMessageService = null)
    {
      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      DeviceCommand cmd = new DeviceCommand(9, 3, 0, 2);
      await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString());
      await Task.Delay(10);
      return true;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectResistor(int numberResistor, IUserInteractionService? userMessageService = null)
    {
      if (numberResistor < 1 || numberResistor > 8)
      {
        return false;
      }

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      DeviceCommand cmd = new DeviceCommand(9, 3, numberResistor, 1);
      await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString());
      await Task.Delay(10);
      return true;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectResistor(int numberResistor, IUserInteractionService? userMessageService = null)
    {
      if (numberResistor < 1 || numberResistor > 8)
      {
        return false;
      }

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      DeviceCommand cmd = new DeviceCommand(9, 3, numberResistor, 2);
      await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString());
      await Task.Delay(10);
      return true;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectCapacitor(int numberCapacitor, IUserInteractionService? userMessageService = null)
    {
      if (numberCapacitor < 1 || numberCapacitor > 6)
      {
        return false;
      }

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      DeviceCommand cmd = new DeviceCommand(9, 3, numberCapacitor + 10, 1);
      await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString());
      await Task.Delay(10);
      return true;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectCapacitor(int numberCapacitor, IUserInteractionService? userMessageService = null)
    {
      if (numberCapacitor < 1 || numberCapacitor > 6)
      {
        return false;
      }

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      DeviceCommand cmd = new DeviceCommand(9, 3, numberCapacitor + 10, 2);
      await _deviceBusCommutation.DeviceProtocol.QueryAsync(cmd.ToString());
      await Task.Delay(10);
      return true;
    }
  }
}
