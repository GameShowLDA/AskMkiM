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
      return true;
    }
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
      _isPaused = false;
    }

    resumeSource?.TrySetResult(true);
  }

  /// <inheritdoc />
  public void Cancel()
  {
    TaskCompletionSource<bool>? resumeSource;
    lock (_syncRoot)
    {
      resumeSource = _resumeSource;
      _isPaused = false;
    }

    resumeSource?.TrySetCanceled();
  }

  /// <inheritdoc />
  public void Reset()
  {
    lock (_syncRoot)
    {
      _isPaused = false;
      _resumeSource = null;
    }
  }

  /// <summary>
  /// Создаёт источник продолжения, выполняющий подписанные продолжения асинхронно.
  /// </summary>
  /// <returns>Новый источник завершения ожидания.</returns>
  private static TaskCompletionSource<bool> CreateResumeSource() =>
    new(TaskCreationOptions.RunContinuationsAsynchronously);
}
