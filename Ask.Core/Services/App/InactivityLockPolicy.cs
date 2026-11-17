using System.Diagnostics;

namespace Ask.Core.Services.App;

/// <summary>Отсчитывает бездействие только вне выполнения рабочих процессов.</summary>
public sealed class InactivityLockPolicy
{
  private long _lastActivity;
  private int _minutes;

  /// <summary>Сбрасывает отсчёт при пользовательском вводе или завершении авторизации.</summary>
  /// <param name="timestamp">Монотонное время по Stopwatch.</param>
  public void Reset(long timestamp) => _lastActivity = timestamp;

  /// <summary>Проверяет срок блокировки с учётом занятости и завершения процесса.</summary>
  /// <param name="timestamp">Текущее монотонное время по Stopwatch.</param>
  /// <param name="minutes">Интервал в минутах; 0 отключает блокировку.</param>
  /// <param name="isBusy">Признак запущенного процесса, включая паузу и завершение.</param>
  /// <param name="lastReleaseTimestamp">Время последнего завершения рабочего процесса.</param>
  /// <returns>Признак истечения времени бездействия.</returns>
  public bool ShouldLock(long timestamp, int minutes, bool isBusy, long lastReleaseTimestamp)
  {
    if (_minutes != minutes)
    {
      _minutes = minutes;
      Reset(timestamp);
    }
    if (isBusy || minutes is not (1 or 2 or 5 or 10))
    {
      Reset(timestamp);
      return false;
    }
    _lastActivity = Math.Max(_lastActivity, lastReleaseTimestamp);
    return Stopwatch.GetElapsedTime(_lastActivity, timestamp) >= TimeSpan.FromMinutes(minutes);
  }
}
