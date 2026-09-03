using Ask.UI.Features.Notifications.Models;
using Ask.UI.Features.Archive.Application;
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
          ShowArchiveNotification("Экспорт архивов", "В папке Archives нет архивов для сохранения на диск.", NotificationType.Warning);
          return;
        }

        ShowArchiveNotification(
          "Экспорт архивов",
          $"Экспортировано архивов: {exportResult.ExportedCount}. Папка: {exportResult.DestinationDirectory}.",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification(
          "Экспорт архивов",
          GetUserFriendlyArchiveTransferErrorMessage(ex, "Не удалось экспортировать архивы на диск."),
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
        var importResult = ArchiveOperationServices.Current.ExecuteImport(
          "Импорт готового архива",
          () => ArchiveTransferService.ImportArchive(archivePath),
          result => new[] { result.ImportedArchivePath });
        var manifestMessage = importResult.ManifestCreated
          ? " Файл с информацией о файлах архива был создан."
          : string.Empty;

        ShowArchiveNotification(
          "Импорт архива",
          $"Архив '{Path.GetFileName(importResult.ImportedArchivePath)}' добавлен в папку Archives.{manifestMessage}",
          NotificationType.Success);
      }
      catch (Exception ex)
      {
        ShowArchiveNotification(
          "Импорт архива",
          GetUserFriendlyArchiveTransferErrorMessage(ex, "Не удалось импортировать архив в папку Archives."),
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
