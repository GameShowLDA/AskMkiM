using System;
using System.Threading.Tasks;
using AppConfiguration.Error.Device;
using NewCore.Base.Device;
using NewCore.Base.Interface.Main;
using NewCore.Device;
using NewCore.Function.Helpers;
using NewCore.Function.Keysight3466new;
using Utilities;
using Utilities.Interface;

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
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserMessageService messageService = null)
    {
      var (connect, answer) = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var result = await _connection.ConnectAsync();

        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Подключение к мультиметру Keysight",
          string.IsNullOrWhiteSpace(result.Answer) ? string.Empty : result.Answer,
          result.Connect,
          1);

        return result;
      }, messageService);

      return (connect, answer);
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserMessageService messageService = null)
    {
      var (connect, answer) = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var result = await _connection.InitializeAsync();

        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Инициализация мультиметра Keysight",
          string.IsNullOrWhiteSpace(result.Answer) ? string.Empty : result.Answer,
          result.Connect,
          1);
        return result;
      }, messageService);

      return (connect, answer);
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync(IUserMessageService messageService = null)
    {
      var connect = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var result = await _connection.DisconnectAsync();

        await DeviceMessageBuilder.ShowConnectionMessageAsync(
          _device,
          "Отключение мультиметра Keysight",
          result ? "Соединение разорвано" : "Ошибка отключения",
          result,
          1);
        return result;
      }, messageService);

      return connect;
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync(IUserMessageService messageService = null)
    {
      var connect = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        return await _connection.ResetAsync();
      }, messageService);

      return connect;
    }
  }
}
