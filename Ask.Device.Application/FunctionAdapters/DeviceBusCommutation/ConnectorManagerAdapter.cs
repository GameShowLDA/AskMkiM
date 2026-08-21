using Ask.Core.Services.Errors.Device.DeviceBusCommutation;
using Ask.Core.Services.UI;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using Ask.Device.Runtime.AskMkiM.Function.DeviceBusCommutation;
using Ask.Device.Runtime.Base.Helpers;

namespace Ask.Device.Application.FunctionAdapters.DeviceBusCommutation
{
  /// <summary>
  /// Адаптер для управления подключением и отключением устройств к шине.
  /// </summary>
  internal class ConnectorManagerAdapter : IConnectorDeviceBusCommutation
  {

    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly ISwitchingDevice _deviceBusCommutation;

    /// <summary>
    /// Менеджер подключения устройств.
    /// </summary>
    private readonly ConnectorManager _connectorManager;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ConnectorManagerAdapter"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public ConnectorManagerAdapter(ISwitchingDevice deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation ?? throw new ArgumentNullException(nameof(deviceBusCommutation));
      _connectorManager = new ConnectorManager((Runtime.Device.SwitchingDevice.DeviceBusCommutation)deviceBusCommutation);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectBreakdownTester(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _connectorManager.ConnectBreakdownTester(userMessageService);

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw ConnectorExceptionFactory.ConnectBreakdownFailed(_deviceBusCommutation.Name, _deviceBusCommutation.NumberChassis, _deviceBusCommutation.Number);
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> EnableDivider(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _connectorManager.EnableDivider(userMessageService);

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw ConnectorExceptionFactory.EnableDividerFailed(_deviceBusCommutation.Name, _deviceBusCommutation.NumberChassis, _deviceBusCommutation.Number);
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisableDivider(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _connectorManager.DisableDivider(userMessageService);

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw ConnectorExceptionFactory.DisableDividerFailed(_deviceBusCommutation.Name, _deviceBusCommutation.NumberChassis, _deviceBusCommutation.Number);
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectBreakdownTester(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _connectorManager.DisconnectBreakdownTester(userMessageService);

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw ConnectorExceptionFactory.DisconnectBreakdownFailed(_deviceBusCommutation.Name, _deviceBusCommutation.NumberChassis, _deviceBusCommutation.Number);
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectMultimeter(SwitchingBusNew bus, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _connectorManager.ConnectMultimeter(bus, userMessageService);

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw ConnectorExceptionFactory.ConnectMultiMeterFailed(_deviceBusCommutation.Name, _deviceBusCommutation.NumberChassis, _deviceBusCommutation.Number);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectMultimeter(SwitchingBusNew bus, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _connectorManager.DisconnectMultimeter(bus, userMessageService);

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw ConnectorExceptionFactory.DisconnectMultiMeterFailed(_deviceBusCommutation.Name, _deviceBusCommutation.NumberChassis, _deviceBusCommutation.Number);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectPINT(SwitchingBusNew bus, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _connectorManager.ConnectPINT(bus, userMessageService);

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw ConnectorExceptionFactory.ConnectPintFailed(_deviceBusCommutation.Name, _deviceBusCommutation.NumberChassis, _deviceBusCommutation.Number);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectPINT(SwitchingBusNew bus, IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _connectorManager.DisconnectPINT(bus, userMessageService);

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw ConnectorExceptionFactory.DisconnectPintFailed(_deviceBusCommutation.Name, _deviceBusCommutation.NumberChassis, _deviceBusCommutation.Number);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectAllBuses(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _connectorManager.ConnectAllBuses(userMessageService);

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw ConnectorExceptionFactory.ConnectAllBusFailed(_deviceBusCommutation.Name, _deviceBusCommutation.NumberChassis, _deviceBusCommutation.Number);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAllBuses(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var succes = await _connectorManager.DisconnectAllBuses(userMessageService);

        return succes;
      }, userMessageService, deviceTask: true);

      if (!result)
        throw ConnectorExceptionFactory.DisconnectAllBusFailed(_deviceBusCommutation.Name, _deviceBusCommutation.NumberChassis, _deviceBusCommutation.Number);

      return result;
    }

    public async Task<bool> ConnectBreakdownTesterAndMultimeter(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        return await _connectorManager.ConnectBreakdownTesterAndMultimeter(userMessageService);
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw ConnectorExceptionFactory.ConnectBreakdownTesterAndMultimeterFailed(_deviceBusCommutation.Name, _deviceBusCommutation.NumberChassis, _deviceBusCommutation.Number);
      }

      return result;
    }

    public async Task<bool> DisconnectBreakdownTesterAndMultimeter(IUserInteractionService? userMessageService = null)
    {
      var result = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        return await _connectorManager.DisconnectBreakdownTesterAndMultimeter(userMessageService);
      }, userMessageService, deviceTask: true);

      if (!result)
      {
        throw ConnectorExceptionFactory.DisconnectBreakdownTesterAndMultimeterFailed(_deviceBusCommutation.Name, _deviceBusCommutation.NumberChassis, _deviceBusCommutation.Number);
      }

      return result;
    }

    public IReadOnlyList<DeviceConnectionInfo> GetConnectedDevices() => _connectorManager.GetConnectedDevices();

  }
}

