using Ask.UI.Features.Notifications.Models;
using Ask.UI.Features.Notifications.ViewModels;
using System.Windows;
using System.Windows.Threading;

namespace Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime
{
  public sealed class NotificationHostService
  {
    private static readonly Lazy<NotificationHostService> InstanceFactory = new(() => new NotificationHostService());

    private static readonly TimeSpan DefaultExpiration = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan CloseAnimationDuration = TimeSpan.FromMilliseconds(1250);
    private static readonly TimeSpan DuplicateSuppressionWindow = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan GlobalRateLimitWindow = TimeSpan.FromSeconds(2);
    private const int MaxPendingShowOperations = 48;
    private const int MaxNotificationsPerRateWindow = 8;

    private readonly WindowNotificationManagerViewModel _viewModel = new();
    private readonly object _notificationsSync = new();
    private readonly Dictionary<string, DateTime> _recentNotificationByKey = new();
    private readonly Queue<DateTime> _acceptedNotificationTimestamps = new();
    private int _pendingShowOperations;

    private NotificationHostService()
    {
    }

    public static NotificationHostService Instance => InstanceFactory.Value;

    public WindowNotificationManagerViewModel ViewModel => _viewModel;

    public void Show(
      string? title,
      string? message,
      NotificationType type = NotificationType.Information,
      TimeSpan? expiration = null)
    {
      if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(message))
      {
        return;
      }

      if (!TryReserveNotificationSlot(title, message, type))
      {
        return;
      }

      var item = new NotificationItemViewModel(title, message, type);
      _ = RunShowPipelineAsync(item, expiration ?? DefaultExpiration);
    }

    public void Dismiss(NotificationItemViewModel item)
    {
      _ = CloseAndRemoveAsync(item);
    }

    public void CloseAll()
    {
      var items = _viewModel.Items.ToList();
      foreach (var item in items)
      {
        _ = CloseAndRemoveAsync(item);
      }
    }

    private async Task RunShowPipelineAsync(NotificationItemViewModel item, TimeSpan expiration)
    {
      try
      {
        await RunOnUiThreadAsync(() =>
        {
          _viewModel.Items.Insert(0, item);

          // Жёстко ограничиваем стек уведомлений.
          // Это защищает UI от лавинообразных вызовов Show().
          while (_viewModel.Items.Count > _viewModel.MaxItems)
          {
            _viewModel.Items.RemoveAt(_viewModel.Items.Count - 1);
          }
        }).ConfigureAwait(false);

        if (expiration <= TimeSpan.Zero)
        {
          return;
        }

        try
        {
          await Task.Delay(expiration).ConfigureAwait(false);
        }
        catch
        {
          return;
        }

        await CloseAndRemoveAsync(item).ConfigureAwait(false);
      }
      finally
      {
        Interlocked.Decrement(ref _pendingShowOperations);
      }
    }

    private async Task CloseAndRemoveAsync(NotificationItemViewModel item)
    {
      var shouldClose = false;

      await RunOnUiThreadAsync(() =>
      {
        if (!_viewModel.Items.Contains(item))
        {
          return;
        }

        shouldClose = item.BeginClose();
      }).ConfigureAwait(false);

      if (!shouldClose)
      {
        return;
      }

      await Task.Delay(CloseAnimationDuration).ConfigureAwait(false);

      await RunOnUiThreadAsync(() =>
      {
        _viewModel.Items.Remove(item);
      }).ConfigureAwait(false);
    }

    private static Task RunOnUiThreadAsync(Action action)
    {
      var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
      if (dispatcher.CheckAccess())
      {
        action();
        return Task.CompletedTask;
      }

      return dispatcher.InvokeAsync(action).Task;
    }

    private bool TryReserveNotificationSlot(string? title, string? message, NotificationType type)
    {
      if (Interlocked.Increment(ref _pendingShowOperations) > MaxPendingShowOperations)
      {
        Interlocked.Decrement(ref _pendingShowOperations);
        return false;
      }

      var now = DateTime.UtcNow;
      var key = $"{type}|{title?.Trim()}|{message?.Trim()}";

      lock (_notificationsSync)
      {
        while (_acceptedNotificationTimestamps.Count > 0 &&
               now - _acceptedNotificationTimestamps.Peek() > GlobalRateLimitWindow)
        {
          _acceptedNotificationTimestamps.Dequeue();
        }

        if (_acceptedNotificationTimestamps.Count >= MaxNotificationsPerRateWindow)
        {
          Interlocked.Decrement(ref _pendingShowOperations);
          return false;
        }

        // Периодически очищаем устаревшие ключи, чтобы словарь не разрастался.
        if (_recentNotificationByKey.Count > 512)
        {
          var staleKeys = _recentNotificationByKey
            .Where(pair => now - pair.Value > DuplicateSuppressionWindow)
            .Select(pair => pair.Key)
            .ToList();

          foreach (var staleKey in staleKeys)
          {
            _recentNotificationByKey.Remove(staleKey);
          }
        }

        if (_recentNotificationByKey.TryGetValue(key, out var lastShownAt) &&
            now - lastShownAt < DuplicateSuppressionWindow)
        {
          Interlocked.Decrement(ref _pendingShowOperations);
          return false;
        }

        _recentNotificationByKey[key] = now;
        _acceptedNotificationTimestamps.Enqueue(now);
      }

      return true;
    }
  }
}
