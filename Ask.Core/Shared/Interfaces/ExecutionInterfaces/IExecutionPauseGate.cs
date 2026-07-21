namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces;

/// <summary>
/// Предоставляет безопасные точки выполнения и ожидания с учётом паузы.
/// </summary>
public interface IExecutionPauseGate
{
  /// <summary>
  /// Ожидает продолжения, если выполнение приостановлено, иначе завершается сразу.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены ожидания.</param>
  /// <returns>Задача, представляющая ожидание продолжения выполнения.</returns>
  /// <exception cref="OperationCanceledException">
  /// Выбрасывается, если запрошена отмена через <paramref name="cancellationToken"/>.
  /// </exception>
  Task WaitIfPausedAsync(CancellationToken cancellationToken);

  /// <summary>
  /// Ожидает указанное время активного выполнения без учёта времени на паузе.
  /// </summary>
  /// <param name="delay">Продолжительность активного ожидания.</param>
  /// <param name="cancellationToken">Токен отмены ожидания.</param>
  /// <returns>Задача, представляющая ожидание указанного интервала.</returns>
  /// <exception cref="OperationCanceledException">
  /// Выбрасывается, если запрошена отмена через <paramref name="cancellationToken"/>.
  /// </exception>
  Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}
