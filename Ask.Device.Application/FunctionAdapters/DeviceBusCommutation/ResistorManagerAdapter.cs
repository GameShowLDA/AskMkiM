using Ask.Core.Services.Errors.Device.DeviceBusCommutation;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.AskMkiM.Function.DeviceBusCommutation;
using Ask.Device.Runtime.Base.Helpers;

namespace Ask.Device.Application.FunctionAdapters.DeviceBusCommutation
{
  /// <summary>
  /// Адаптер управления подключением/отключением резисторов.
  /// </summary>
  internal class ResistorManagerAdapter : IResistorDeviceBusCommutation
  {
    private readonly Runtime.Device.SwitchingDevice.DeviceBusCommutation _deviceBusCommutation;
    private readonly ResistorManager _resistorManager;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ResistorManagerAdapter"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public ResistorManagerAdapter(Runtime.Device.SwitchingDevice.DeviceBusCommutation deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation ?? throw new ArgumentNullException(nameof(deviceBusCommutation));
      _resistorManager = new ResistorManager(deviceBusCommutation);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectResistor(string number, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _resistorManager.ConnectResistor(number, userMessageService);

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw ResistorExceptionFactory.ConnectFailed(number);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectResistor(string number, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _resistorManager.DisconnectResistor(number, userMessageService);

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw ResistorExceptionFactory.DisconnectFailed(number);

      return result;
    }
  }
}

