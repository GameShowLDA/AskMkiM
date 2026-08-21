using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.ResponseProcessor.DeviceBusCommutation.ResponseProcessing;
using Ask.Device.Runtime.AskMkiM.Base.Commands;
using static Ask.LogLib.LoggerUtility;

namespace Ask.Device.Runtime.AskMkiM.Function.DeviceBusCommutation
{
  /// <summary>
  /// Менеджер управления коммутацией резисторов.
  /// Обеспечивает подключение и отключение резисторов в системе.
  /// </summary>
  public class ResistorManager : IResistorDeviceBusCommutation
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
    public ResistorManager(Device.SwitchingDevice.DeviceBusCommutation deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation;
      queryExecutor = new DeviceBusCommutationQueryExecutor(deviceBusCommutation);
    }

    /// <summary>
    /// Замыкание резистора.
    /// </summary>
    /// <param name="number">Номер резистора.</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public async Task<bool> ConnectResistor(string number, IUserInteractionService? userMessageService = null)
    {
      if (int.TryParse(number, out int num))
      {
        DeviceCommand cmd = new DeviceCommand(6, 1, num, 1);
        string answer = await queryExecutor.QueryAsync(cmd.ToString());
        return await DeviceBusCommutationResponseProcessor.CheckChainOperationAsync(
          answer, _deviceBusCommutation, true, "резистора", $"№{number}", userMessageService);
      }

      LogError("Неверный номер резистора!", isDeviceLog: true);
      return false;
    }

    /// <summary>
    /// Размыкание резистора.
    /// </summary>
    /// <param name="number">Номер резистора.</param>
    /// <returns>Задача (Task), представляющая асинхронную операцию.</returns>
    public async Task<bool> DisconnectResistor(string number, IUserInteractionService? userMessageService = null)
    {
      if (int.TryParse(number, out int num))
      {
        DeviceCommand cmd = new DeviceCommand(6, 1, num, 2);
        string answer = await queryExecutor.QueryAsync(cmd.ToString());
        return await DeviceBusCommutationResponseProcessor.CheckChainOperationAsync(
          answer, _deviceBusCommutation, false, "резистора", $"№{number}", userMessageService);
      }

      LogError("Неверный номер резистора!", isDeviceLog: true);
      return false;
    }
  }
}


