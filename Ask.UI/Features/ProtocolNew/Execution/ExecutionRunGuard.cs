namespace Ask.UI.Features.ProtocolNew.Execution;

/// <summary>
/// Потокобезопасная реализация глобального ограничения на один активный процесс выполнения.
/// </summary>
internal sealed class ExecutionRunGuard : IExecutionRunGuard
{
  /// <summary>
  /// Объект синхронизации доступа к глобальному слоту.
  /// </summary>
  private static readonly object SyncRoot = new();

  /// <summary>
  /// Владелец текущего активного слота.
  /// </summary>
  private static object? _activeOwner;

  /// <summary>
  /// Имя текущего активного процесса.
  /// </summary>
  private static string _activeProcessName = string.Empty;

  /// <inheritdoc />
  public bool TryAcquire(string processName, object owner, out string activeProcessName)
  {
    lock (SyncRoot)
    {
      if (_activeOwner != null && !ReferenceEquals(_activeOwner, owner))
      {
        activeProcessName = _activeProcessName;
        return false;
      }

      _activeOwner = owner;
      _activeProcessName = processName;
      activeProcessName = string.Empty;
      return true;
    }
  }

  /// <inheritdoc />
  public void Release(object owner)
  {
    lock (SyncRoot)
    {
      if (!ReferenceEquals(_activeOwner, owner))
      {
        return;
      }

      _activeOwner = null;
      _activeProcessName = string.Empty;
    }
  }
}
