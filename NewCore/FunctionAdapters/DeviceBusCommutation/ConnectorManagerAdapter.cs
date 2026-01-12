using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.Errors.Device.DeviceBusCommutation;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice.Capabilities;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using NewCore.Function.DeviceBusCommutation;
using NewCore.Function.Helpers;

namespace NewCore.FunctionAdapters.DeviceBusCommutation
{
  /// <summary>
  /// Адаптер для управления подключением и отключением устройств к шине.
  /// </summary>
  internal class ConnectorManagerAdapter : IConnectorDeviceBusCommutation
  {
    private enum DeviceType
    {
      Multimeter,
      PINT,
    }

    private bool IsBreadownConnect = false;
    private bool IsBreakdownTesterAndMultimeter = false;

    /// <summary>
    /// Устройство коммутации шин.
    /// </summary>
    private readonly ISwitchingDevice _deviceBusCommutation;

    /// <summary>
    /// Менеджер подключения устройств.
    /// </summary>
    private readonly ConnectorManager _connectorManager;

    private Dictionary<(DeviceType, SwitchingBusNew), bool> deviceBusStatus = new Dictionary<(DeviceType, SwitchingBusNew), bool>();

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ConnectorManagerAdapter"/>.
    /// </summary>
    /// <param name="deviceBusCommutation">Экземпляр устройства коммутации шин.</param>
    public ConnectorManagerAdapter(ISwitchingDevice deviceBusCommutation)
    {
      _deviceBusCommutation = deviceBusCommutation ?? throw new ArgumentNullException(nameof(deviceBusCommutation));
      _connectorManager = new ConnectorManager((Device.DeviceBusCommutation)deviceBusCommutation);
      _deviceBusCommutation.ConnectableManager.IsReset += ConnectableManager_IsReset;
    }

    /// <inheritdoc />
    private void ConnectableManager_IsReset()
    {
      deviceBusStatus.Clear();

      foreach (DeviceType item in System.Enum.GetValues(typeof(DeviceType)))
      {
        foreach (SwitchingBusNew item2 in System.Enum.GetValues(typeof(SwitchingBusNew)))
        {
          deviceBusStatus.Add((item, item2), false);
        }
      }
    }

    /// <inheritdoc />
    public async Task<bool> ConnectBreakdownTester(IUserInteractionService? userMessageService = null)
    {
      if (IsBreadownConnect)
        return true;

      var result = await _connectorManager.ConnectBreakdownTester();

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, "Подключение пробойной установки", result, 1, userMessageService);
      }

      if (!result)
      {
        throw ConnectorExceptionFactory.ConnectFailed("пробойной установки");
      }
      else
      {
        IsBreadownConnect = true;
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectBreakdownTester(IUserInteractionService? userMessageService = null)
    {
      if (!IsBreadownConnect)
        return true;

      var result = await _connectorManager.DisconnectBreakdownTester();

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, "Отключение пробойной установки", result, 1, userMessageService);
      }

      if (!result)
      {
        throw ConnectorExceptionFactory.DisconnectFailed("пробойной установки");
      }
      else
      {
        IsBreadownConnect = false;
      }

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectMultimeter(SwitchingBusNew bus, IUserInteractionService? userMessageService = null)
    {
      var description = $"мультиметра к шине [{bus}]";

      // Проверка: подключен ли мультиметр уже к этой шине
      if (deviceBusStatus.TryGetValue((DeviceType.Multimeter, bus), out var isConnected) && isConnected)
        return true;

      // Найдём все активные подключения мультиметра к другим шинам
      foreach (var kvp in deviceBusStatus.Where(x => x.Key.Item1 == DeviceType.Multimeter && x.Value))
      {
        // Отключаем старую шину
        var oldBus = kvp.Key.Item2;
        var disconnectResult = await DisconnectMultimeter(oldBus);
        deviceBusStatus[(DeviceType.Multimeter, oldBus)] = false;
      }

      var result = await _connectorManager.ConnectMultimeter(bus);

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, $"Подключение {description}", result, 1, userMessageService);
      }

      if (!result)
        throw ConnectorExceptionFactory.ConnectFailed(description);

      deviceBusStatus[(DeviceType.Multimeter, bus)] = true;
      return result;
    }


    /// <inheritdoc />
    public async Task<bool> DisconnectMultimeter(SwitchingBusNew bus, IUserInteractionService? userMessageService = null)
    {
      if (deviceBusStatus.TryGetValue((DeviceType.Multimeter, bus), out var isConnected) && !isConnected)
        return true;

      var description = $"мультиметра с шины [{bus}]";
      var result = await _connectorManager.DisconnectMultimeter(bus);

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, $"Отключение {description}", result, 1, userMessageService);
      }

      if (!result)
        throw ConnectorExceptionFactory.DisconnectFailed(description);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectPINT(SwitchingBusNew bus, IUserInteractionService? userMessageService = null)
    {
      var description = $"ПИНТ к шине [{bus}]";

      // Проверка: подключен ли ПИНТ уже к этой шине
      if (deviceBusStatus.TryGetValue((DeviceType.PINT, bus), out var isConnected) && isConnected)
        return true;

      // Найдём все активные подключения ПИНТ к другим шинам
      foreach (var kvp in deviceBusStatus.Where(x => x.Key.Item1 == DeviceType.PINT && x.Value))
      {
        var oldBus = kvp.Key.Item2;
        var disconnectResult = await _connectorManager.DisconnectPINT(oldBus);
        deviceBusStatus[(DeviceType.PINT, oldBus)] = false;
      }

      var result = await _connectorManager.ConnectPINT(bus);

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, $"Подключение {description}", result, 1, userMessageService);
      }

      if (!result)
        throw ConnectorExceptionFactory.ConnectFailed(description);

      deviceBusStatus[(DeviceType.PINT, bus)] = true;
      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectPINT(SwitchingBusNew bus, IUserInteractionService? userMessageService = null)
    {
      if (deviceBusStatus.TryGetValue((DeviceType.Multimeter, bus), out var isConnected) && !isConnected)
        return true;

      var description = $"ПИНТ с шины [{bus}]";
      var result = await _connectorManager.DisconnectPINT(bus);

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, $"Отключение {description}", result, 1, userMessageService);
      }

      if (!result)
        throw ConnectorExceptionFactory.DisconnectFailed(description);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> ConnectAllBuses(IUserInteractionService? userMessageService = null)
    {
      var description = $"(AB1, AB2, AB3, AB4)";
      var result = await _connectorManager.ConnectAllBuses();

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, $"Подключение {description}", result, 1, userMessageService);
      }

      if (!result)
        throw ConnectorExceptionFactory.DisconnectFailed(description);

      return result;
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAllBuses(IUserInteractionService? userMessageService = null)
    {
      var description = $"(AB1, AB2, AB3, AB4)";
      var result = await _connectorManager.DisconnectAllBuses();

      if (!result || await DeviceDisplayConfig.GetConnectionInfoVisibilityAsync())
      {
        await DeviceMessageBuilder.ShowConnectionMessageAsync(_deviceBusCommutation, $"Отключение {description}", result, 1, userMessageService);
      }

      if (!result)
        throw ConnectorExceptionFactory.DisconnectFailed(description);

      return result;
    }

    public async Task<bool> GetSuccesCurrentMode(SwitchingDeviceTypeConnector mode, IUserInteractionService? userMessageService = null)
    {
      var result = await _connectorManager.GetSuccesCurrentMode(mode);
      return result;
    }

    public async Task<bool> ConnectBreakdownTesterAndMultimeter(IUserInteractionService? userMessageService = null)
    {
      if (IsBreakdownTesterAndMultimeter)
        return true;

      var result = await _connectorManager.ConnectBreakdownTesterAndMultimeter(userMessageService);
      if (result)
      {
        IsBreakdownTesterAndMultimeter = true;
      }
      return result;
    }

    public async Task<bool> DisconnectBreakdownTesterAndMultimeter(IUserInteractionService? userMessageService = null)
    {
      if (!IsBreakdownTesterAndMultimeter)
        return true;

      var result = await _connectorManager.DisconnectBreakdownTesterAndMultimeter(userMessageService);
      if (result)
      {
        IsBreakdownTesterAndMultimeter = false;
      }
      return result;
    }
  }
}
