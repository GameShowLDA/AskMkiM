using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.UI;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;
using NewCore.Device;
using NewCore.Function.Helpers;
using NewCore.Function.Keysight3466new;

namespace NewCore.FunctionAdapters.Keysight3466new
{
  /// <summary>
  /// Адаптер подключения к мультиметру Keysight с отображением сообщений.
  /// </summary>
  internal class KeysightConnectionAdapter : IConnectable
  {
    private readonly KeysightDevice _device;
    private readonly KeysightConnection _connection;

    public event Action DeviceDisponce;
    public event Action IsReset;

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
    public async Task<(bool Connect, string Answer)> ConnectAsync(IUserInteractionService messageService = null)
    {
      var (connect, answer) = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var result = await _connection.ConnectAsync();

        if (!result.Connect || await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Подключение к мультиметру Keysight", string.IsNullOrWhiteSpace(result.Answer) ? string.Empty : result.Answer, result.Connect, 1, messageService);
        }

        return result;
      }, messageService);

      return (connect, answer);
    }

    /// <inheritdoc />
    public async Task<(bool Connect, string Answer)> InitializeAsync(IUserInteractionService messageService = null)
    {
      var (connect, answer) = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var result = await _connection.InitializeAsync();

        if (!result.Connect || await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Инициализация мультиметра Keysight", string.IsNullOrWhiteSpace(result.Answer) ? string.Empty : result.Answer, result.Connect, 1, messageService);
        }

        return result;
      }, messageService);

      return (connect, answer);
    }

    /// <inheritdoc />
    public async Task<bool> DisconnectAsync(IUserInteractionService messageService = null)
    {
      var connect = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        var result = await _connection.DisconnectAsync();

        if (!result || await DeviceDisplayConfig.GetExecutionParametersVisibilityAsync())
        {
          await DeviceMessageBuilder.ShowConnectionMessageAsync(_device, "Отключение мультиметра Keysight", result ? "Соединение разорвано" : "Ошибка отключения", result, 1, messageService);
        }

        return result;
      }, messageService);

      return connect;
    }

    /// <inheritdoc />
    public async Task<bool> ResetAsync(IUserInteractionService messageService = null)
    {
      var connect = await UserActionHelper.GetRunWithUserRepeatAsync(async () =>
      {
        return await _connection.ResetAsync();
      }, messageService);

      IsReset?.Invoke();

      return connect;
    }

    public string GetConnectionStatus()
    {
      throw new NotImplementedException();
    }
  }
}
