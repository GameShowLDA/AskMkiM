using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Commands;
using Ask.Device.Runtime.Function.Helpers;

namespace Ask.Device.Runtime.Function.DeviceBusCommutation
{
  /// <summary>
  /// Менеджер управления подключением конденсаторов.
  /// Обеспечивает подключение и отключение конденсаторов в системе.
  /// </summary>
  public class CapacitorManager : ICapacitorDeviceBusCommutation
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
    public CapacitorManager(Device.DeviceBusCommutation deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation;
      queryExecutor = new DeviceBusCommutationQueryExecutor(deviceBusCommutation);
    }

    /// <summary>
    /// Замыкание конденсатора.
    /// </summary>
    /// <param name="number">Номер конденсатора.</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public async Task<bool> ConnectCapacitor(int number, IUserInteractionService? userMessageService = null)
    {
      DeviceCommand command = new DeviceCommand(6, 2, number, 1);
      string answer = await queryExecutor.QueryAsync(command.ToString());
      return !ExecutionConfig.GetIsIdleModeEnabled() || !string.IsNullOrWhiteSpace(answer);
    }

    /// <summary>
    /// Размыкание конденсатора.
    /// </summary>
    /// <param name="number">Номер конденсатора.</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public async Task<bool> DisconnectCapacitor(int number, IUserInteractionService? userMessageService = null)
    {
      DeviceCommand command = new DeviceCommand(6, 2, number, 2);
      string answer = await queryExecutor.QueryAsync(command.ToString());
      return !ExecutionConfig.GetIsIdleModeEnabled() || !string.IsNullOrWhiteSpace(answer);
    }
  }
}
