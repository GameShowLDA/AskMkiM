using Ask.Core.Services.Errors.Device.DeviceBusCommutation;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Device.Runtime.Function.DeviceBusCommutation;
using Ask.Device.Runtime.Function.Helpers;

namespace Ask.Device.Application.FunctionAdapters.DeviceBusCommutation
{
  /// <summary>
  /// Адаптер управления подключением/отключением реле.
  /// </summary>
  internal class RelayManagerAdapter : IRelayDeviceBusCommutation
  {
    private readonly Runtime.Device.DeviceBusCommutation _deviceBusCommutation;
    private readonly RelayManager _relayManager;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="RelayManagerAdapter"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public RelayManagerAdapter(Runtime.Device.DeviceBusCommutation deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation ?? throw new ArgumentNullException(nameof(deviceBusCommutation));
      _relayManager = new RelayManager(deviceBusCommutation);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectRelay(int numberRelay, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _relayManager.ConnectRelay(numberRelay, userMessageService);

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw RelayControlExceptionFactory.ConnectFailed(numberRelay);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectRelay(int numberRelay, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _relayManager.DisconnectRelay(numberRelay, userMessageService);

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw RelayControlExceptionFactory.DisconnectFailed(numberRelay);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> EnableRelay(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _relayManager.EnableRelay(userMessageService);

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw RelayControlExceptionFactory.EnableFailed();

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisableRelay(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _relayManager.DisableRelay(userMessageService);

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw RelayControlExceptionFactory.DisableFailed();

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectRCRelay(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(
        async () =>
        {
          var succes = await _relayManager.ConnectRCRelay(userMessageService);

          return succes;
        },
        userMessageService!,
        deviceTask: true);

      if (!result)
        throw RelayControlExceptionFactory.ConnectRCFailed();

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectRCRelay(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(
        async () =>
        {
          var succes = await _relayManager.DisconnectRCRelay(userMessageService);

          return succes;
        },
        userMessageService!,
        deviceTask: true);

      if (!result)
        throw RelayControlExceptionFactory.DisconnectRCFailed();

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectResistor(int numberResistor, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(
        async () =>
        {
          var succes = await _relayManager.ConnectResistor(numberResistor, userMessageService);

          return succes;
        },
        userMessageService!,
        deviceTask: true);

      if (!result)
        throw ResistorExceptionFactory.ConnectFailed($"R{numberResistor}");

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectResistor(int numberResistor, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(
        async () =>
        {
          var succes = await _relayManager.DisconnectResistor(numberResistor, userMessageService);

          return succes;
        },
        userMessageService!,
        deviceTask: true);

      if (!result)
        throw ResistorExceptionFactory.DisconnectFailed($"R{numberResistor}");

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectCapacitor(int numberCapacitor, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(
        async () =>
        {
          var succes = await _relayManager.ConnectCapacitor(numberCapacitor, userMessageService);

          return succes;
        },
        userMessageService!,
        deviceTask: true);

      if (!result)
        throw CapacitorExceptionFactory.ConnectFailed($"C{numberCapacitor}");

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectCapacitor(int numberCapacitor, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(
        async () =>
        {
          var succes = await _relayManager.DisconnectCapacitor(numberCapacitor, userMessageService);

          return succes;
        },
        userMessageService!,
        deviceTask: true);

      if (!result)
        throw CapacitorExceptionFactory.DisconnectFailed($"C{numberCapacitor}");

      return result;
    }
  }
}
