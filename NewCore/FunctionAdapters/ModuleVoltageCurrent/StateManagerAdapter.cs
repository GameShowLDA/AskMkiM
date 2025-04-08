using System;
using System.Threading.Tasks;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;
using NewCore.Device;
using NewCore.Function.Helpers;
using NewCore.Function.ModuleVoltageCurrentSource;

namespace NewCore.FunctionAdapters.ModuleVoltageCurrentSource
{
  /// <summary>
  /// Адаптер для управления состоянием МИНТ с отображением сообщений.
  /// </summary>
  internal class StateManagerAdapter : IConnectable
  {
    private readonly IPowerSourceModule _device;
    private readonly StateManager _stateManager;

    public StateManagerAdapter(IPowerSourceModule device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _stateManager = new StateManager(device);
    }

    public async Task<(bool Connect, string Answer)> ConnectAsync()
    {
      var (success, message) = await _stateManager.ConnectAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Подключение", message, success, 1);
      return (success, message);
    }

    public async Task<bool> DisconnectAsync()
    {
      bool success = await _stateManager.DisconnectAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Отключение", success, 1);
      return success;
    }

    public async Task<(bool Connect, string Answer)> InitializeAsync()
    {
      var (success, message) = await _stateManager.InitializeAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Инициализация", message, success, 1);
      return (success, message);
    }

    public async Task<bool> ResetAsync()
    {
      bool success = await _stateManager.ResetAsync();
      await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Сброс", success, 1);
      return success;
    }
  }
}
