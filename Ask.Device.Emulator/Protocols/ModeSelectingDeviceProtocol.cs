using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;

namespace Ask.Device.Emulator.Protocols
{
  /// <summary>
  /// Выбирает реальный или эмулируемый протокол в зависимости от режима выполнения.
  /// </summary>
  internal sealed class ModeSelectingDeviceProtocol : IDeviceProtocol
  {
    private readonly Func<IDeviceProtocol?> _realProtocolProvider;
    private readonly IDeviceProtocol _emulatorProtocol;

    /// <summary>
    /// Инициализирует переключатель реального и эмулируемого протоколов.
    /// </summary>
    /// <param name="realProtocolProvider">Функция получения текущего реального протокола устройства.</param>
    /// <param name="emulatorProtocol">Протокол эмулятора устройства.</param>
    public ModeSelectingDeviceProtocol(
      Func<IDeviceProtocol?> realProtocolProvider,
      IDeviceProtocol emulatorProtocol)
    {
      _realProtocolProvider = realProtocolProvider
        ?? throw new ArgumentNullException(nameof(realProtocolProvider));
      _emulatorProtocol = emulatorProtocol
        ?? throw new ArgumentNullException(nameof(emulatorProtocol));
    }

    /// <inheritdoc />
    public SemaphoreSlim OperationLock { get; set; } = new(1, 1);

    /// <inheritdoc />
    public Task<string> QueryAsync(
      string command,
      double responseDelay = 0,
      int timeout = 0,
      int port = 0,
      int delayBeforeCall = 0,
      CancellationToken cancellationToken = default)
    {
      IDeviceProtocol protocol = ExecutionConfig.GetIsIdleModeEnabled()
        ? _emulatorProtocol
        : _realProtocolProvider()
          ?? throw new InvalidOperationException("Реальный протокол устройства не инициализирован.");

      return protocol.QueryAsync(
        command,
        responseDelay,
        timeout,
        port,
        delayBeforeCall,
        cancellationToken);
    }
  }
}