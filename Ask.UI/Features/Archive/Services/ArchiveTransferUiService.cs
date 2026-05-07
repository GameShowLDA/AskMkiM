using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using System.IO;
using System.Windows;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using OpenFolderDialog = Microsoft.Win32.OpenFolderDialog;
using Path = System.IO.Path;

namespace Ask.UI.Features.Archive.Services
{
  /// <summary>
  /// Предоставляет UI-операции для импорта и экспорта архивов.
  /// </summary>
  public static class ArchiveTransferUiService
  {
    /// <summary>
    /// Выполняет экспорт всех архивов в выбранную пользователем директорию.
    /// </summary>
    public static void DownloadArchives()
    {
      var targetFolder = SelectFolder("Выберите папку для скачивания архивов");
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

    /// <summary>
    /// Выполняет импорт архива в директорию Archives.
    /// </summary>
    public static void UploadArchive()
    {
      var openFileDialog = new OpenFileDialog
      {
        Title = "Загрузить архив",
        Filter = "Архив ASK (*.apkw)|*.apkw",
        CheckFileExists = true,
        Multiselect = false,
      };

      if (!ShowDialog(openFileDialog))
      {
        return;
      }

      try
      {
        var importResult = ArchiveTransferService.ImportArchive(openFileDialog.FileName);
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

    /// <summary>
    /// Отображает диалог выбора директории.
    /// </summary>
    /// <param name="title">Заголовок диалога.</param>
    /// <returns>Путь к выбранной директории или null.</returns>
    private static string? SelectFolder(string title)
    {
      var dialog = new OpenFolderDialog
      {
        Title = title,
        Multiselect = false,
        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
      };

      return ShowDialog(dialog)
        ? dialog.FolderName
        : null;
    }

    /// <summary>
    /// Отображает системный диалог с привязкой к главному окну приложения.
    /// </summary>
    /// <param name="dialog">Диалоговое окно.</param>
    /// <returns>
    /// true, если пользователь подтвердил выбор;
    /// иначе false.
    /// </returns>
    private static bool ShowDialog(Microsoft.Win32.CommonDialog dialog)
    {
      var owner = Application.Current?.MainWindow;
      return owner != null
        ? dialog.ShowDialog(owner) == true
        : dialog.ShowDialog() == true;
    }

    /// <summary>
    /// Отображает уведомление о выполнении операций с архивами.
    /// </summary>
    /// <param name="title">Заголовок уведомления.</param>
    /// <param name="message">Текст уведомления.</param>
    /// <param name="notificationType">Тип уведомления.</param>
    private static void ShowArchiveNotification(string title, string message, NotificationType notificationType)
    {
      NotificationHostService.Instance.Show(title, message, notificationType);
    }

    /// <summary>
    /// Преобразует исключение в понятное пользователю сообщение об ошибке импорта или экспорта архивов.
    /// </summary>
    /// <param name="ex">Возникшее исключение.</param>
    /// <param name="fallbackMessage">Резервное сообщение об ошибке.</param>
    /// <returns>Сообщение об ошибке для отображения пользователю.</returns>
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
