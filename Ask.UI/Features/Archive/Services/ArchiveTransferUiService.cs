using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using System.IO;
using Path = System.IO.Path;

namespace Ask.UI.Features.Archive.Services
{
  public static class ArchiveTransferUiService
  {
    public static void DownloadArchives()
    {
      var targetFolder = ArchiveFileDialogService.SelectFolder(ownerElement: null, "Выберите папку для скачивания архивов");
      if (string.IsNullOrWhiteSpace(targetFolder))
      {
        return;
      }

      try
      {
        var exportResult = ArchiveTransferService.ExportAllArchives(targetFolder);
        if (exportResult.ExportedCount == 0 || string.IsNullOrWhiteSpace(exportResult.DestinationDirectory))
        {
          ShowArchiveNotification("Скачивание архивов", "В папке Archives нет архивов для сохранения на диск.", NotificationType.Warning);
          return;
        }

        ShowArchiveNotification(
          "Скачивание архивов",
          $"Скачано архивов: {exportResult.ExportedCount}. Папка: {exportResult.DestinationDirectory}.",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification(
          "Скачивание архивов",
          GetUserFriendlyArchiveTransferErrorMessage(ex, "Не удалось скачать архивы на диск."),
          NotificationType.Error);
      }
    }

    public static void UploadArchive()
    {
      var archivePath = ArchiveFileDialogService.SelectArchiveImportFile(ownerElement: null);
      if (string.IsNullOrWhiteSpace(archivePath))
      {
        return;
      }

      try
      {
        var importResult = ArchiveTransferService.ImportArchive(archivePath);
        var manifestMessage = importResult.ManifestCreated
          ? " Файл с информацией о файлах архива был создан."
          : string.Empty;

        ShowArchiveNotification(
          "Загрузка архива",
          $"Архив '{Path.GetFileName(importResult.ImportedArchivePath)}' добавлен в папку Archives.{manifestMessage}",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification(
          "Загрузка архива",
          GetUserFriendlyArchiveTransferErrorMessage(ex, "Не удалось загрузить архив в папку Archives."),
          NotificationType.Error);
      }
    }

    private static void ShowArchiveNotification(string title, string message, NotificationType notificationType)
    {
      NotificationHostService.Instance.Show(title, message, notificationType);
    }

    private static string GetUserFriendlyArchiveTransferErrorMessage(Exception ex, string fallbackMessage)
    {
      if (ex is InvalidOperationException invalidOperation && !string.IsNullOrWhiteSpace(invalidOperation.Message))
      {
        return invalidOperation.Message;
      }

      if (ex is FileNotFoundException fileNotFoundException && !string.IsNullOrWhiteSpace(fileNotFoundException.Message))
      {
        return fileNotFoundException.Message;
      }

      if (ex is DirectoryNotFoundException directoryNotFoundException && !string.IsNullOrWhiteSpace(directoryNotFoundException.Message))
      {
        return directoryNotFoundException.Message;
      }

      if (ex is InvalidDataException invalidDataException && !string.IsNullOrWhiteSpace(invalidDataException.Message))
      {
        return invalidDataException.Message;
      }

      if (ex is IOException ioException && !string.IsNullOrWhiteSpace(ioException.Message))
      {
        return ioException.Message;
      }

      if (ex is ArgumentException argumentException && !string.IsNullOrWhiteSpace(argumentException.Message))
      {
        return argumentException.Message;
      }

      return fallbackMessage;
    }
  }
}
