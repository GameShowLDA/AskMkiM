using Ask.UI.Features.Notifications.Models;
using Ask.UI.Shared.ViewModels;

namespace Ask.UI.Features.Notifications.ViewModels
{
  public sealed class NotificationItemViewModel : ObservableObject
  {
    private bool _isClosing;

    public NotificationItemViewModel(string? title, string? message, NotificationType type)
    {
      Title = title;
      Message = message;
      Type = type;
    }

    public string? Title { get; }

    public string? Message { get; }

    public NotificationType Type { get; }

    public bool IsClosing
    {
      get => _isClosing;
      private set => SetProperty(ref _isClosing, value);
    }

    public bool BeginClose()
    {
      if (IsClosing)
      {
        return false;
      }

      IsClosing = true;
      return true;
    }
  }
}
