using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.ResponseProcessor.DeviceBusCommutation.ResponseProcessing;
using Ask.Device.Runtime.AskMkiM.Base.Commands;
using Ask.Device.Runtime.Base.Helpers;

namespace Ask.Device.Runtime.AskMkiM.Function.DeviceBusCommutation
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
    private readonly Device.SwitchingDevice.DeviceBusCommutation _deviceBusCommutation;
    private readonly DeviceBusCommutationQueryExecutor queryExecutor;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="BusManager"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public CapacitorManager(Device.SwitchingDevice.DeviceBusCommutation deviceBusCommutation)
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
      return await DeviceBusCommutationResponseProcessor.CheckChainOperationAsync(
        answer, _deviceBusCommutation, true, "конденсатора", number.ToString(), userMessageService);
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
      return await DeviceBusCommutationResponseProcessor.CheckChainOperationAsync(
        answer, _deviceBusCommutation, false, "конденсатора", number.ToString(), userMessageService);
    }
  }
}


