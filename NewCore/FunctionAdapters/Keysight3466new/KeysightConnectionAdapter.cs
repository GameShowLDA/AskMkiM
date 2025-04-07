using System;
using System.Threading.Tasks;
using NewCore.Device;
using NewCore.Function.Helpers;
using NewCore.Function.Keysight3466new;
using NewCore.Base.Interface.Main;
using NewCore.Base.Device;

namespace NewCore.FunctionAdapters.Keysight3466new
{
  /// <summary>
  /// Адаптер подключения к мультиметру Keysight с отображением сообщений.
  /// </summary>
  internal class KeysightConnectionAdapter : IConnectable
  {
    private readonly KeysightDevice _device;
    private readonly KeysightConnection _connection;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="KeysightConnectionAdapter"/>.
    /// </summary>
    /// <param name="device">Экземпляр устройства Keysight.</param>
    public KeysightConnectionAdapter(KeysightDevice device)
    {
      _device = device ?? throw new ArgumentNullException(nameof(device));
      _connection = new KeysightConnection(device);
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> ConnectAsync()
    {
      var (result, answer) = await _connection.ConnectAsync();

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _device,
        "Подключение к мультиметру Keysight",
        string.IsNullOrWhiteSpace(answer) ? "Успешно" : answer,
        result,
        1);

      return (result, answer);
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync()
    {
      var (result, answer) = await _connection.InitializeAsync();

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _device,
        "Инициализация мультиметра Keysight",
        string.IsNullOrWhiteSpace(answer) ? "Готов к работе" : answer,
        result,
        1);

      return (result, answer);
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync()
    {
      var result = await _connection.DisconnectAsync();

      await DeviceMessageBuilder.ShowConnectionMessageAsync(
        _device,
        "Отключение мультиметра Keysight",
        result ? "Соединение разорвано" : "Ошибка отключения",
        result,
        1);

      return result;
    }

    /// <inheritdoc />
    public Task<bool> ResetAsync()
    {
      // Для Keysight reset ничего не делает, просто обёртка:
      return _connection.ResetAsync();
    }
  }
}
