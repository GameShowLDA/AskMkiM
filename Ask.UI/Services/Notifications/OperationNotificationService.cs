using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;

namespace Ask.UI.Services.Notifications
{
  public static class OperationNotificationService
  {
    public static void ShowSuccess(string title, string message)
    {
      Show(title, message, NotificationType.Success);
    }

    public static void ShowWarning(string title, string message)
    {
      Show(title, message, NotificationType.Warning);
    }

    public static void ShowError(string title, string message)
    {
      Show(title, message, NotificationType.Error);
    }

    private static void Show(string title, string message, NotificationType type)
    {
      NotificationHostService.Instance.Show(title, message, type);
    }
  }
}
