using Ask.Core.Shared.Interfaces.DeviceInterfaces;

namespace Ask.Device.Communication.Common;

/// <summary>
/// Ограничивает время ожидания операций реального протокола оборудования.
/// </summary>
public sealed class HardwareWatchdogProtocol : IDeviceProtocol
{
  private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
  private readonly IDeviceProtocol _innerProtocol;
  private readonly string _deviceName;
  private readonly TimeSpan _timeout;

  /// <summary>
  /// Инициализирует защитный протокол для указанного транспорта.
  /// </summary>
  /// <param name="innerProtocol">Реальный транспортный протокол.</param>
  /// <param name="deviceName">Наименование оборудования.</param>
  /// <param name="timeout">Максимальное время выполнения одной операции.</param>
  public HardwareWatchdogProtocol(
    IDeviceProtocol innerProtocol,
    string deviceName,
    TimeSpan? timeout = null)
  {
    _innerProtocol = innerProtocol ?? throw new ArgumentNullException(nameof(innerProtocol));
    _deviceName = string.IsNullOrWhiteSpace(deviceName) ? "Оборудование" : deviceName;
    _timeout = timeout ?? DefaultTimeout;

    if (_timeout <= TimeSpan.Zero)
    {
      throw new ArgumentOutOfRangeException(nameof(timeout), _timeout, "Тайм-аут должен быть больше нуля.");
    }
  }

  /// <inheritdoc />
  public SemaphoreSlim OperationLock
  {
    get => _innerProtocol.OperationLock;
    set => _innerProtocol.OperationLock = value;
  }

  /// <inheritdoc />
  public async Task<string> QueryAsync(
    string command,
    double responseDelay = 0,
    int timeout = 0,
    int port = 0,
    int delayBeforeCall = 0,
    CancellationToken cancellationToken = default)
  {
    using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    Task<string> operation = Task.Run(
      () => _innerProtocol.QueryAsync(
        command,
        responseDelay,
        timeout,
        port,
        delayBeforeCall,
        operationCancellation.Token),
      CancellationToken.None);

    try
    {
      return await operation.WaitAsync(_timeout, cancellationToken).ConfigureAwait(false);
    }
    catch (TimeoutException ex)
    {
      await operationCancellation.CancelAsync().ConfigureAwait(false);
      throw new TimeoutException(
        $"{_deviceName} не завершило команду \"{command}\" за {_timeout.TotalSeconds:0.###} с.",
        ex);
    }
  }
}
