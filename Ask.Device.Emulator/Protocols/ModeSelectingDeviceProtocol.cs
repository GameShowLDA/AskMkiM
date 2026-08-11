using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;

namespace Ask.Device.Emulator.Protocols
{
  /// <summary>
  /// Выбирает реальный или эмулируемый протокол в зависимости от режима выполнения.
  /// </summary>
  internal sealed class ModeSelectingDeviceProtocol : IDeviceProtocol
  {
    private static readonly TimeSpan DefaultHardwareOperationTimeout = TimeSpan.FromSeconds(5);
    private readonly Func<IDeviceProtocol?> _realProtocolProvider;
    private readonly IDeviceProtocol _emulatorProtocol;
    private readonly TimeSpan _hardwareOperationTimeout;

    /// <summary>
    /// Инициализирует переключатель реального и эмулируемого протоколов.
    /// </summary>
    /// <param name="realProtocolProvider">Функция получения текущего реального протокола устройства.</param>
    /// <param name="emulatorProtocol">Протокол эмулятора устройства.</param>
    public ModeSelectingDeviceProtocol(
      Func<IDeviceProtocol?> realProtocolProvider,
      IDeviceProtocol emulatorProtocol,
      TimeSpan? hardwareOperationTimeout = null)
    {
      _realProtocolProvider = realProtocolProvider
        ?? throw new ArgumentNullException(nameof(realProtocolProvider));
      _emulatorProtocol = emulatorProtocol
        ?? throw new ArgumentNullException(nameof(emulatorProtocol));
      _hardwareOperationTimeout = hardwareOperationTimeout ?? DefaultHardwareOperationTimeout;

      if (_hardwareOperationTimeout <= TimeSpan.Zero)
      {
        throw new ArgumentOutOfRangeException(
          nameof(hardwareOperationTimeout),
          _hardwareOperationTimeout,
          "Тайм-аут аппаратной операции должен быть больше нуля.");
      }
    }

    /// <inheritdoc />
    public SemaphoreSlim OperationLock { get; set; } = new(1, 1);

    /// <inheritdoc />
    public async Task<string> QueryAsync(
      string command,
      double responseDelay = 0,
      int timeout = 0,
      int port = 0,
      int delayBeforeCall = 0,
      CancellationToken cancellationToken = default)
    {
      bool isIdleMode = ExecutionConfig.GetIsIdleModeEnabled();
      IDeviceProtocol protocol = isIdleMode
        ? _emulatorProtocol
        : _realProtocolProvider()
          ?? throw new InvalidOperationException("Реальный протокол устройства не инициализирован.");

      if (isIdleMode)
      {
        return await protocol.QueryAsync(
          command,
          responseDelay,
          timeout,
          port,
          delayBeforeCall,
          cancellationToken);
      }

      using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      Task<string> queryTask = Task.Run(
        () => protocol.QueryAsync(
          command,
          responseDelay,
          timeout,
          port,
          delayBeforeCall,
          operationCancellation.Token),
        CancellationToken.None);

      return await WaitForHardwareResponseAsync(
        queryTask,
        command,
        operationCancellation,
        cancellationToken);
    }

    private async Task<string> WaitForHardwareResponseAsync(
      Task<string> queryTask,
      string command,
      CancellationTokenSource operationCancellation,
      CancellationToken cancellationToken)
    {
      try
      {
        return await queryTask
          .WaitAsync(_hardwareOperationTimeout, cancellationToken)
          .ConfigureAwait(false);
      }
      catch (TimeoutException ex)
      {
        await operationCancellation.CancelAsync().ConfigureAwait(false);
        throw new TimeoutException(
          $"Оборудование не завершило команду \"{command}\" за {_hardwareOperationTimeout.TotalSeconds:0.###} с.",
          ex);
      }
    }
  }
}
