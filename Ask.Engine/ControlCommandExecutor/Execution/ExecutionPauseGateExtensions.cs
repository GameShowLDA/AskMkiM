using Ask.Core.Shared.Interfaces.ExecutionInterfaces;
using Ask.Core.Shared.Interfaces.UiInterfaces;

namespace Ask.Engine.ControlCommandExecutor.Execution;

/// <summary>
/// Централизует точки проверки и ожидания паузы для исполнителей команд.
/// </summary>
internal static class ExecutionPauseGateExtensions
{
  /// <summary>
  /// Ожидает продолжения, если выполнение приостановлено.
  /// </summary>
  /// <param name="interactionService">Сервис взаимодействия с пользователем.</param>
  /// <returns>Задача, представляющая ожидание продолжения выполнения.</returns>
  public static Task WaitIfPausedAsync(this IUserInteractionService interactionService)
  {
    var cancellationToken = interactionService.GetCancellationToken();
    return interactionService is IExecutionPauseGate pauseGate
      ? pauseGate.WaitIfPausedAsync(cancellationToken)
      : Task.CompletedTask;
  }

  /// <summary>
  /// Ожидает указанный интервал активного выполнения без учёта времени на паузе.
  /// </summary>
  /// <param name="interactionService">Сервис взаимодействия с пользователем.</param>
  /// <param name="delay">Продолжительность активного ожидания.</param>
  /// <returns>Задача, представляющая ожидание указанного интервала.</returns>
  public static Task DelayWithPauseAsync(
    this IUserInteractionService interactionService,
    TimeSpan delay)
  {
    var cancellationToken = interactionService.GetCancellationToken();
    return interactionService is IExecutionPauseGate pauseGate
      ? pauseGate.DelayAsync(delay, cancellationToken)
      : Task.Delay(delay, cancellationToken);
  }
}
