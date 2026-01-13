using Ask.Core.Services.Errors.Device.DeviceBusCommutation;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Function.DeviceBusCommutation;
using NewCore.Function.Helpers;

namespace NewCore.FunctionAdapters.DeviceBusCommutation
{
  /// <summary>
  /// Адаптер управления подключением/отключением резисторов.
  /// </summary>
  internal class ResistorManagerAdapter : IResistorDeviceBusCommutation
  {
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;
    private readonly ResistorManager _resistorManager;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ResistorManagerAdapter"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public ResistorManagerAdapter(Device.DeviceBusCommutation deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation ?? throw new ArgumentNullException(nameof(deviceBusCommutation));
      _resistorManager = new ResistorManager(deviceBusCommutation);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectResistor(string number, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _resistorManager.ConnectResistor(number), userMessageService, deviceTask: true);

      await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, "Подключение резистора", $"№{number}", result, 1, userMessageService);

      if (!result)
        throw ResistorExceptionFactory.ConnectFailed(number);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectResistor(string number, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(() => _resistorManager.DisconnectResistor(number), userMessageService, deviceTask: true);

      await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, "Отключение резистора", $"№{number}", result, 1, userMessageService);

      if (!result)
        throw ResistorExceptionFactory.DisconnectFailed(number);

      return result;
    }
  }
}
