using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.DeviceBusCommutation;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Function.DeviceBusCommutation;
using Ask.Device.Runtime.Function.Helpers;

namespace Ask.Device.Application.FunctionAdapters.DeviceBusCommutation
{
  /// <summary>
  /// Адаптер управления подключением/отключением резисторов.
  /// </summary>
  internal class ResistorManagerAdapter : IResistorDeviceBusCommutation
  {
    private readonly Runtime.Device.DeviceBusCommutation _deviceBusCommutation;
    private readonly ResistorManager _resistorManager;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ResistorManagerAdapter"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public ResistorManagerAdapter(Runtime.Device.DeviceBusCommutation deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation ?? throw new ArgumentNullException(nameof(deviceBusCommutation));
      _resistorManager = new ResistorManager(deviceBusCommutation);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectResistor(string number, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _resistorManager.ConnectResistor(number);

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, "Подключение резистора", $"№{number}", succes, 1, userMessageService);
        }

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
        var succes = await _resistorManager.DisconnectResistor(number);

        if (!succes || DeviceDisplayConfig.GetConnectionInfoVisibility())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, "Отключение резистора", $"№{number}", succes, 1, userMessageService);
        }

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw ResistorExceptionFactory.DisconnectFailed(number);

      return result;
    }
  }
}
