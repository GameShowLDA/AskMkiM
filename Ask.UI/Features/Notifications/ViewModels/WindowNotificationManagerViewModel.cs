using Ask.UI.Shared.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;

namespace Ask.UI.Features.Notifications.ViewModels
{
  public sealed class WindowNotificationManagerViewModel : ObservableObject
  {
    private int _maxItems = 3;
    private Thickness _hostMargin = new(0, 52, 10, 0);

    public ObservableCollection<NotificationItemViewModel> Items { get; } = new();

    public int MaxItems
    {
      get => _maxItems;
      set => SetProperty(ref _maxItems, value);
    }

    public Thickness HostMargin
    {
      get => _hostMargin;
      set => SetProperty(ref _hostMargin, value);
    }
  }
}
