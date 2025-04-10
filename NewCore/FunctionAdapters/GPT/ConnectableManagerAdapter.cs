using System;
using System.Threading.Tasks;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;
using NewCore.Device;
using NewCore.Function.GPT;
using NewCore.Function.Helpers;
using Utilities.Error.Device;

namespace NewCore.FunctionAdapters.GPT
{
  /// <summary>
  /// Адаптер для управления соединением с GPT-79904 с выводом сообщений.
  /// </summary>
  internal class ConnectableManagerAdapter : IConnectable
  {
    private readonly GPT79904 _device;
    private readonly ConnectableManager _manager;

    public ConnectableManagerAdapter(GPT79904 device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _manager = new ConnectableManager(device);
    }

    public async Task<(bool Connect, string Answer)> ConnectAsync()
    {
      var (result, answer) = await _manager.ConnectAsync();

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Подключение пробойной установки",
          string.IsNullOrWhiteSpace(answer) ? "Успешно" : answer,
          result,
          1);

      if (!result)
        throw ConnectionExceptionFactory.ConnectFailed(_device.Name, _device.NumberChassis, _device.Number, answer);

      return (result, answer);
    }

    public async Task<bool> DisconnectAsync()
    {
      var result = await _manager.DisconnectAsync();

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Отключение пробойной установки",
          result ? "Успешно" : "Ошибка отключения",
          result,
          1);

      if (!result)
        throw ConnectionExceptionFactory.DisconnectFailed(_device.Name, _device.NumberChassis, _device.Number);

      return result;
    }

    public async Task<(bool Connect, string Answer)> InitializeAsync()
    {
      var (result, answer) = await _manager.InitializeAsync();

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Инициализация пробойной установки",
          string.IsNullOrWhiteSpace(answer) ? "ОК" : answer,
          result,
          1);

      if (!result)
        throw ConnectionExceptionFactory.InitializeFailed(_device.Name, _device.NumberChassis, _device.Number, answer);

      return (result, answer);
    }

    public async Task<bool> ResetAsync()
    {
      var result = await _manager.ResetAsync();

      if (!result)
        throw ConnectionExceptionFactory.ResetFailed(_device.Name, _device.NumberChassis, _device.Number);

      return result;
    }
  }
}
