namespace Ask.Core.Services.UI
{
  /// <summary>
  /// Обозначает выполнение одной попытки команды программы контроля.
  /// </summary>
  public static class ControlProgramCommandExecutionContext
  {
    private static readonly AsyncLocal<CommandScope?> CurrentScope = new();

    /// <summary>
    /// Признак выполнения команды программы контроля.
    /// </summary>
    public static bool IsActive => CurrentScope.Value != null;

    /// <summary>
    /// Настройка повтора измерения, зафиксированная для текущей команды.
    /// </summary>
    public static bool? RepeatMeasurementEnabled => CurrentScope.Value?.RepeatMeasurementEnabled;

    /// <summary>
    /// Настройка остановки при ошибке, зафиксированная для текущей команды.
    /// </summary>
    public static bool? StopOnErrorEnabled => CurrentScope.Value?.StopOnErrorEnabled;

    /// <summary>
    /// Возвращает текущую настройку остановки при ошибке.
    /// </summary>
    /// <returns><see langword="true"/>, если выполнение останавливается при ошибке.</returns>
    public static bool GetCurrentStopOnErrorEnabled() =>
      Ask.Core.Services.Config.AppSettings.ExecutionConfig.GetStopOnErrorEnabled();

    /// <summary>
    /// Подавляет интерактивные остановки внутри текущей команды.
    /// </summary>
    /// <returns>Область выполнения команды.</returns>
    public static IDisposable Enter()
    {
      var previousScope = CurrentScope.Value;
      CurrentScope.Value = new CommandScope(
        previousScope,
        previousScope?.RepeatMeasurementEnabled
          ?? Ask.Core.Services.Config.AppSettings.ExecutionConfig.GetIsRepeatMeasurementEnabled(),
        previousScope?.StopOnErrorEnabled
          ?? Ask.Core.Services.Config.AppSettings.ExecutionConfig.GetStopOnErrorEnabled());
      return new Scope(previousScope);
    }

    private sealed record CommandScope(
      CommandScope? Parent,
      bool RepeatMeasurementEnabled,
      bool StopOnErrorEnabled);

    private sealed class Scope : IDisposable
    {
      private readonly CommandScope? _previousScope;
      private bool _disposed;

      public Scope(CommandScope? previousScope) => _previousScope = previousScope;

      public void Dispose()
      {
        if (_disposed)
        {
          return;
        }

        _disposed = true;
        CurrentScope.Value = _previousScope;
      }
    }
  }
}
