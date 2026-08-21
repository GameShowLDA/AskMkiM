using Ask.Core.Shared.Interfaces.ExecutionInterfaces;

namespace Ask.UI.Features.ProtocolNew.Execution;

/// <summary>
/// Потокобезопасно координирует паузу и продолжение через <see cref="TaskCompletionSource{TResult}"/>.
/// </summary>
internal sealed class ExecutionPauseController : IExecutionPauseController
{
  /// <summary>
  /// Объект синхронизации состояния паузы.
  /// </summary>
  private readonly object _syncRoot = new();

  /// <summary>
  /// Источник завершения текущего ожидания паузы.
  /// </summary>
  private TaskCompletionSource<bool>? _resumeSource;

  /// <summary>
  /// Источник сигнала запроса паузы.
  /// </summary>
  private TaskCompletionSource<bool> _pauseSource = CreateSignalSource();

  /// <summary>
  /// Текущее состояние паузы.
  /// </summary>
  private bool _isPaused;

  /// <inheritdoc />
  public bool IsPaused
  {
    get
    {
      lock (_syncRoot)
      {
        return _isPaused;
      }
    }
  }

  /// <inheritdoc />
  public bool RequestPause()
  {
    lock (_syncRoot)
    {
      if (_isPaused && _resumeSource is { Task.IsCompleted: false })
      {
        return false;
      }

      _isPaused = true;
      _resumeSource = CreateResumeSource();
      _pauseSource.TrySetResult(true);
      return true;
    }
  }

  /// <inheritdoc />
  public async Task WaitForPauseRequestAsync(CancellationToken cancellationToken)
  {
    Task waitTask;
    lock (_syncRoot)
    {
      if (_isPaused)
      {
        return;
      }

      waitTask = _pauseSource.Task;
    }

    await waitTask.WaitAsync(cancellationToken).ConfigureAwait(false);
  }

  /// <inheritdoc />
  public async Task WaitAsync(CancellationToken cancellationToken)
  {
    Task waitTask;
    lock (_syncRoot)
    {
      if (!_isPaused)
      {
        return;
      }

      _resumeSource ??= CreateResumeSource();
      waitTask = _resumeSource.Task;
    }

    await waitTask.WaitAsync(cancellationToken);
  }

  /// <inheritdoc />
  public void Resume()
  {
    TaskCompletionSource<bool>? resumeSource;
    lock (_syncRoot)
    {
      resumeSource = _resumeSource;
      _resumeSource = null;
      _isPaused = false;
      _pauseSource = CreateSignalSource();
    }

    resumeSource?.TrySetResult(true);
  }

  /// <summary>
  /// Прерывает текущее ожидание, сохраняя состояние паузы.
  /// </summary>
  /// <returns>
  /// <see langword="true"/>, если ожидание было прервано.
  /// В противном случае — <see langword="false"/>.
  /// </returns>
  public bool InterruptWait()
  {
    TaskCompletionSource<bool>? resumeSource;
    lock (_syncRoot)
    {
      if (!_isPaused)
      {
        return false;
      }

      resumeSource = _resumeSource;
      _resumeSource = CreateResumeSource();
    }

    resumeSource?.TrySetResult(true);
    return true;
  }

  /// <inheritdoc />
  public void Cancel()
  {
    TaskCompletionSource<bool>? resumeSource;
    lock (_syncRoot)
    {
      resumeSource = _resumeSource;
      _resumeSource = null;
      _isPaused = false;
      _pauseSource = CreateSignalSource();
    }

    resumeSource?.TrySetCanceled();
  }

  /// <inheritdoc />
  public void Reset()
  {
    TaskCompletionSource<bool>? resumeSource;
    lock (_syncRoot)
    {
      resumeSource = _resumeSource;
      _isPaused = false;
      _resumeSource = null;
      _pauseSource = CreateSignalSource();
    }

    resumeSource?.TrySetResult(true);
  }

  /// <summary>
  /// Создаёт источник продолжения, выполняющий подписанные продолжения асинхронно.
  /// </summary>
  /// <returns>Новый источник завершения ожидания.</returns>
  private static TaskCompletionSource<bool> CreateResumeSource() =>
    new(TaskCreationOptions.RunContinuationsAsynchronously);

  /// <summary>
  /// Создаёт источник сигнала запроса паузы.
  /// </summary>
  /// <returns>Новый источник сигнала запроса паузы.</returns>
  private static TaskCompletionSource<bool> CreateSignalSource() =>
    new(TaskCreationOptions.RunContinuationsAsynchronously);
}
