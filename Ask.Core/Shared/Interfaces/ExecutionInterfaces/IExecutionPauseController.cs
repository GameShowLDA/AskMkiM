namespace Ask.Core.Shared.Interfaces.ExecutionInterfaces;

/// <summary>
/// Определяет операции управления приостановкой, продолжением и отменой ожидания выполнения.
/// </summary>
public interface IExecutionPauseController
{
  /// <summary>
  /// Возвращает признак активной паузы.
  /// </summary>
  bool IsPaused { get; }

  /// <summary>
  /// Регистрирует запрос паузы.
  /// </summary>
  /// <returns><see langword="true"/>, если пауза была запрошена впервые.</returns>
  bool RequestPause();

  /// <summary>
  /// Асинхронно ожидает запроса паузы.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены ожидания.</param>
  /// <returns>Задача, представляющая ожидание запроса паузы.</returns>
  /// <exception cref="OperationCanceledException">
  /// Выбрасывается, если запрошена отмена через <paramref name="cancellationToken"/>.
  /// </exception>
  Task WaitForPauseRequestAsync(CancellationToken cancellationToken);

  /// <summary>
  /// Асинхронно ожидает продолжения или отмены выполнения.
  /// </summary>
  /// <param name="cancellationToken">Токен отмены ожидания.</param>
  Task WaitAsync(CancellationToken cancellationToken);

  /// <summary>
  /// Снимает выполнение с паузы.
  /// </summary>
  void Resume();

  /// <summary>
  /// Отменяет текущее ожидание паузы.
  /// </summary>
  void Cancel();

  /// <summary>
  /// Возвращает контроллер в начальное состояние перед новым запуском.
  /// </summary>
  void Reset();
}
