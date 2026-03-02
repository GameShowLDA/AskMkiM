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

    private readonly WindowNotificationManagerViewModel _viewModel = new();

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
      await RunOnUiThreadAsync(() =>
      {
        _viewModel.Items.Insert(0, item);

        while (_viewModel.Items.Count > _viewModel.MaxItems)
        {
          var overflowItem = _viewModel.Items[^1];
          _ = CloseAndRemoveAsync(overflowItem);
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

    private async Task CloseAndRemoveAsync(NotificationItemViewModel item)
    {
      var shouldClose = false;

      await RunOnUiThreadAsync(() =>
      {
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
  }
}
