using Ask.Core.Shared.DTO.Devices.Base;
using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;

namespace UI.Controls.Settings.DeviceConfig;

internal static class DeviceConfigNotifications
{
  private const string ConfigurationTitle = "Конфигурация оборудования";

  public static void ShowCreated(DeviceDto device)
  {
    NotificationHostService.Instance.Show(
      ConfigurationTitle,
      $"{FormatDevice(device)} создано",
      NotificationType.Success);
  }

  public static void ShowUpdated(DeviceDto device)
  {
    NotificationHostService.Instance.Show(
      ConfigurationTitle,
      $"{FormatDevice(device)} изменено",
      NotificationType.Success);
  }

  public static void ShowDeleted(DeviceDto device)
  {
    NotificationHostService.Instance.Show(
      ConfigurationTitle,
      $"{FormatDevice(device)} удалено",
      NotificationType.Success);
  }

  public static void ShowDeleteError(DeviceDto device, Exception exception)
  {
    NotificationHostService.Instance.Show(
      "Ошибка изменения конфигурации",
      $"Не удалось удалить {FormatDevice(device)}: {exception.Message}",
      NotificationType.Error);
  }

  private static string FormatDevice(DeviceDto device)
  {
    string name = string.IsNullOrWhiteSpace(device.Name) ? "Устройство" : device.Name.Trim();
    return $"{name} ({device.Number})";
  }
}
