using System.Threading;

namespace Ask.Core.Services.App
{
  /// <summary>
  /// Единый источник системного времени для интерфейса приложения.
  /// Обновляет время по границе секунды и уведомляет подписчиков.
  /// </summary>
  public static class ApplicationClockService
  {
    private static readonly object SyncRoot = new();

    private static Timer? _timer;
    private static bool _isRunning;
    private static DateTime _currentDateTime = TrimToSecond(DateTime.Now);

    public static event Action<DateTime>? TimeChanged;

    public static DateTime CurrentDateTime
    {
      get
      {
        lock (SyncRoot)
        {
          return _currentDateTime;
        }
      }
    }

    public static void Start()
    {
      Action<DateTime>? handlers;
      DateTime currentDateTime;

      lock (SyncRoot)
      {
        if (_isRunning)
        {
          return;
        }

        _isRunning = true;
        _currentDateTime = TrimToSecond(DateTime.Now);
        _timer = new Timer(OnTimerTick);
        ScheduleNextTickUnsafe();

        handlers = TimeChanged;
        currentDateTime = _currentDateTime;
      }

      handlers?.Invoke(currentDateTime);
    }

    public static void Stop()
    {
      lock (SyncRoot)
      {
        _isRunning = false;
        _timer?.Dispose();
        _timer = null;
      }
    }

    private static void OnTimerTick(object? state)
    {
      Action<DateTime>? handlers;
      DateTime currentDateTime;

      lock (SyncRoot)
      {
        if (!_isRunning)
        {
          return;
        }

        currentDateTime = TrimToSecond(DateTime.Now);

        if (currentDateTime != _currentDateTime)
        {
          _currentDateTime = currentDateTime;
        }

        ScheduleNextTickUnsafe();
        handlers = TimeChanged;
      }

      handlers?.Invoke(currentDateTime);
    }

    private static void ScheduleNextTickUnsafe()
    {
      if (_timer == null)
      {
        return;
      }

      var now = DateTime.Now;
      var nextTick = TrimToSecond(now).AddSeconds(1);
      var dueTime = nextTick - now;

      if (dueTime < TimeSpan.FromMilliseconds(10))
      {
        dueTime = TimeSpan.FromMilliseconds(10);
      }

      _timer.Change(dueTime, Timeout.InfiniteTimeSpan);
    }

    private static DateTime TrimToSecond(DateTime value)
    {
      return new DateTime(
        value.Year,
        value.Month,
        value.Day,
        value.Hour,
        value.Minute,
        value.Second,
        value.Kind);
    }
  }
}
