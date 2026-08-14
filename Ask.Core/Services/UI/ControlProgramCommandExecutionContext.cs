namespace Ask.Core.Services.UI
{
  /// <summary>
  /// Обозначает выполнение одной попытки команды программы контроля.
  /// </summary>
  public static class ControlProgramCommandExecutionContext
  {
    private static readonly AsyncLocal<int> ScopeDepth = new();

    /// <summary>
    /// Признак выполнения команды программы контроля.
    /// </summary>
    public static bool IsActive => ScopeDepth.Value > 0;

    /// <summary>
    /// Подавляет интерактивные остановки внутри текущей команды.
    /// </summary>
    /// <returns>Область выполнения команды.</returns>
    public static IDisposable Enter()
    {
      ScopeDepth.Value++;
      return new Scope();
    }

    private sealed class Scope : IDisposable
    {
      private bool _disposed;

      public void Dispose()
      {
        if (_disposed)
        {
          return;
        }

        _disposed = true;
        ScopeDepth.Value = Math.Max(0, ScopeDepth.Value - 1);
      }
    }
  }
}
