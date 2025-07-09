using System;
using System.Threading.Tasks;
using AppConfiguration.Error.Device.DeviceBusCommutation;
using NewCore.Enum;
using NewCore.Function.DeviceBusCommutation;
using NewCore.Function.Helpers;

namespace NewCore.FunctionAdapters.DeviceBusCommutation
{
  /// <summary>
  /// Адаптер для управления подключением и отключением устройств к шине.
  /// </summary>
  internal class ConnectorManagerAdapter : IConnectorDeviceBusCommutation
  {
    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly Device.DeviceBusCommutation _deviceBusCommutation;

    /// <summary>
    /// Менеджер подключения устройств.
    /// </summary>
    private readonly ConnectorManager _connectorManager;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ConnectorManagerAdapter"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public ConnectorManagerAdapter(Device.DeviceBusCommutation deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation ?? throw new ArgumentNullException(nameof(deviceBusCommutation));
      _connectorManager = new ConnectorManager(deviceBusCommutation);
    }

    /// <inheritdoc />
    public async Task<bool> ConnectBreakdownTester()
    {
      var result = await _connectorManager.ConnectBreakdownTester();

      await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, "Подключение пробойной установки", result, 1);

      if (!result)
        throw ConnectorExceptionFactory.ConnectFailed("пробойной установки");

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectBreakdownTester()
    {
      var result = await _connectorManager.DisconnectBreakdownTester();

      await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, "Отключение пробойной установки", result, 1);

      if (!result)
        throw ConnectorExceptionFactory.DisconnectFailed("пробойной установки");

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectMultimeter(DeviceEnum.SwitchingBusNew bus)
    {
      var description = $"мультиметра к шине [{bus}]";
      var result = await _connectorManager.ConnectMultimeter(bus);

      await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, $"Подключение {description}", result, 1);

      if (!result)
        throw ConnectorExceptionFactory.ConnectFailed(description);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectMultimeter(DeviceEnum.SwitchingBusNew bus)
    {
      var description = $"мультиметра с шины [{bus}]";
      var result = await _connectorManager.DisconnectMultimeter(bus);

      await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, $"Отключение {description}", result, 1);

      if (!result)
        throw ConnectorExceptionFactory.DisconnectFailed(description);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectPINT(DeviceEnum.SwitchingBusNew bus)
    {
      var description = $"ПИНТ к шине [{bus}]";
      var result = await _connectorManager.ConnectPINT(bus);

      await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, $"Подключение {description}", result, 1);

      if (!result)
        throw ConnectorExceptionFactory.ConnectFailed(description);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectPINT(DeviceEnum.SwitchingBusNew bus)
    {
      var description = $"ПИНТ с шины [{bus}]";
      var result = await _connectorManager.DisconnectPINT(bus);

      await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, $"Отключение {description}", result, 1);

      if (!result)
        throw ConnectorExceptionFactory.DisconnectFailed(description);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectAllBuses()
    {
      var description = $"(AB1, AB2, AB3, AB4)";
      var result = await _connectorManager.ConnectAllBuses();

      await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, $"Подключение {description}", result, 1);

      if (!result)
        throw ConnectorExceptionFactory.DisconnectFailed(description);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAllBuses()
    {
      var description = $"(AB1, AB2, AB3, AB4)";
      var result = await _connectorManager.DisconnectAllBuses();

      await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, $"Отключение {description}", result, 1);

      if (!result)
        throw ConnectorExceptionFactory.DisconnectFailed(description);

      return result;
    }
  }
}
