using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Services.Errors.Models;
using Ask.Core.Services.FilesUtility;
using Ask.Core.Shared.DTO.Executor;
using Ask.Core.Shared.Metadata.Static;
using Ask.Engine.ControlCommandAnalyser;
using Ask.UI.Features.Archive.Views;
using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Media;
using static Ask.LogLib.LoggerUtility;

namespace Ask.UI.Features.Archive.Services
{
  public sealed class ArchiveSaveService
  {
    public bool SaveFileToArchive(FrameworkElement ownerElement, List<BaseCommandModel> models, string sourceFilePath)
    {
      ArgumentNullException.ThrowIfNull(ownerElement);

      try
      {
        if (models == null || models.Count == 0)
        {
          LogWarning("Нет данных для сохранения в архив.");
          ShowArchiveNotification(
            "Сохранение в архив",
            "Нет данных для сохранения в архив.",
            NotificationType.Warning);

          return false;
        }

        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
          LogError("Не удалось определить имя файла для сохранения.");
          ShowArchiveNotification(
            "Сохранение в архив",
            "Не удалось определить имя файла для сохранения.",
            NotificationType.Error);

          return false;
        }

        var manager = new CommandTranslationManager();
        var modelList = models.ToList();
        var sourceLines = new List<List<string>>(modelList.Count);

        foreach (var model in modelList)
        {
          sourceLines.Add(manager.GetSourceLines(model, out int _));
        }

        var fileName = Path.GetFileNameWithoutExtension(sourceFilePath) + ".opkw";
        var archivePath = GetArchivePathForSave(ownerElement, Path.GetFileNameWithoutExtension(fileName));

        if (string.IsNullOrWhiteSpace(archivePath))
        {
          return false;
        }

        using var archiveManager = new ArchiveManager();
        archiveManager.OpenArchive(archivePath);
        archiveManager.AddFileToArchive(sourceLines, archivePath, fileName);

        LogInformation($"Файл {fileName} добавлен в архив '{Path.GetFileNameWithoutExtension(archivePath)}'.");
        ShowArchiveNotification(
          "Сохранение в архив",
          $"Файл {fileName} добавлен в архив '{Path.GetFileNameWithoutExtension(archivePath)}'.",
          NotificationType.Success);

        return true;
      }
      catch (Exception ex)
      {
        var message = GetUserFriendlySaveErrorMessage(ex);
        LogError(message);
        ShowArchiveNotification("Сохранение в архив", message, NotificationType.Error);
        return false;
      }
    }

    public bool SaveFileToArchive(FrameworkElement ownerElement, string translatedText, string sourceFilePath)
    {
      ArgumentNullException.ThrowIfNull(ownerElement);

      try
      {
        if (string.IsNullOrWhiteSpace(translatedText))
        {
          LogWarning("Нет данных для сохранения в архив.");
          ShowArchiveNotification(
            "Сохранение в архив",
            "Нет данных для сохранения в архив.",
            NotificationType.Warning);

          return false;
        }

        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
          LogError("Не удалось определить имя файла для сохранения.");
          ShowArchiveNotification(
            "Сохранение в архив",
            "Не удалось определить имя файла для сохранения.",
            NotificationType.Error);

          return false;
        }

        var normalizedText = translatedText.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalizedText.Split('\n').ToList();
        var sourceLines = new List<List<string>> { lines };

        var fileName = Path.GetFileNameWithoutExtension(sourceFilePath) + ".opkw";
        var archivePath = GetArchivePathForSave(ownerElement, Path.GetFileNameWithoutExtension(fileName));

        if (string.IsNullOrWhiteSpace(archivePath))
        {
          return false;
        }

        using var archiveManager = new ArchiveManager();
        archiveManager.OpenArchive(archivePath);
        archiveManager.AddFileToArchive(sourceLines, archivePath, fileName);

        LogInformation($"Файл {fileName} добавлен в архив '{Path.GetFileNameWithoutExtension(archivePath)}'.");
        ShowArchiveNotification(
          "Сохранение в архив",
          $"Файл {fileName} добавлен в архив '{Path.GetFileNameWithoutExtension(archivePath)}'.",
          NotificationType.Success);

        return true;
      }
      catch (Exception ex)
      {
        var message = GetUserFriendlySaveErrorMessage(ex);
        LogError(message);
        ShowArchiveNotification("Сохранение в архив", message, NotificationType.Error);
        return false;
      }
    }

    private string? GetArchivePathForSave(FrameworkElement ownerElement, string suggestedArchiveName)
    {
      var archivesFolderPath = ArchiveDirectoryService.ResolveArchivesRootPath();
      var archivePaths = ArchiveDirectoryService.GetArchivesInDirectory(archivesFolderPath);
      return PromptForArchiveSelection(ownerElement, archivePaths, archivesFolderPath, suggestedArchiveName);
    }

    private string? PromptForArchiveSelection(
      FrameworkElement ownerElement,
      IReadOnlyList<string> archivePaths,
      string archivesFolderPath,
      string suggestedArchiveName)
    {
      var dialog = CreateDialogWindow(ownerElement, "Сохранение в архив");
      var content = new ArchiveSaveTargetPickerControl();
      content.Initialize(ownerElement, archivePaths);

      content.CreateArchiveRequested += (_, _) =>
      {
        while (true)
        {
          var archiveName = PromptForArchiveName(
            ownerElement,
            suggestedArchiveName,
            isFirstArchive: archivePaths.Count == 0 && string.IsNullOrWhiteSpace(content.SelectedArchivePath));

          if (string.IsNullOrWhiteSpace(archiveName))
          {
            return;
          }

          try
          {
            var createdArchivePath = CreateArchiveInFolder(archivesFolderPath, archiveName);
            content.AddArchive(createdArchivePath);
            return;
          }
          catch (Exception ex)
          {
            var message = GetUserFriendlyCreateArchiveErrorMessage(ex);
            LogError(message);
            ShowArchiveNotification("Создание архива", message, NotificationType.Error);
          }
        }
      };

      content.ConfirmRequested += (_, _) =>
      {
        if (!string.IsNullOrWhiteSpace(content.SelectedArchivePath))
        {
          dialog.DialogResult = true;
          return;
        }

        const string message = "Выберите архив из списка или создайте новый.";
        LogWarning(message);
        ShowArchiveNotification("Сохранение в архив", message, NotificationType.Warning);
      };

      dialog.Content = content;

      if (dialog.ShowDialog() != true)
      {
        return null;
      }

      return content.SelectedArchivePath;
    }

    private string? PromptForArchiveName(FrameworkElement ownerElement, string suggestedArchiveName, bool isFirstArchive = false)
    {
      var dialog = CreateDialogWindow(ownerElement, "Создание архива");
      var content = new ArchiveNameInputControl();
      content.Initialize(ownerElement, suggestedArchiveName, isFirstArchive);
      content.ConfirmRequested += (_, _) => dialog.DialogResult = true;
      dialog.Content = content;

      return dialog.ShowDialog() == true
        ? content.ArchiveName
        : null;
    }

    private static Window CreateDialogWindow(FrameworkElement ownerElement, string title)
    {
      return new Window
      {
        Title = title,
        Owner = Window.GetWindow(ownerElement),
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
        ResizeMode = ResizeMode.NoResize,
        SizeToContent = SizeToContent.WidthAndHeight,
        ShowInTaskbar = false,
        WindowStyle = WindowStyle.None,
        AllowsTransparency = true,
        Background = Brushes.Transparent,
      };
    }

    private static string CreateArchiveInFolder(string archivesFolderPath, string archiveName)
    {
      var normalizedArchiveName = NormalizeArchiveName(archiveName);
      var archivePath = Path.Combine(archivesFolderPath, normalizedArchiveName + ".apkw");

      if (File.Exists(archivePath))
      {
        var message = $"Архив '{Path.GetFileName(archivePath)}' уже существует.";
        LogError(message);
        throw new InvalidOperationException(message);
      }

      using (var archiveStream = new FileStream(archivePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
      using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Update, leaveOpen: false))
      {
        ArchiveManifestService.WriteManifest(archive, new List<ArchiveManifestFileRecord>());
      }

      FileEncryptionManager.EncryptFile(archivePath);
      EventAggregator.Publish(new ArchiveEvents.Changed(ArchiveEvents.ArchiveChangeKind.ArchiveCreated, archivePath));

      return archivePath;
    }

    private static string NormalizeArchiveName(string archiveName)
    {
      if (string.IsNullOrWhiteSpace(archiveName))
      {
        const string message = "Название архива обязательно.";
        LogError(message);
        throw new ArgumentException(message, nameof(archiveName));
      }

      var normalizedName = Path.GetFileNameWithoutExtension(archiveName.Trim());
      foreach (var invalidChar in Path.GetInvalidFileNameChars())
      {
        normalizedName = normalizedName.Replace(invalidChar, '_');
      }

      if (string.IsNullOrWhiteSpace(normalizedName))
      {
        const string message = "Название архива содержит только недопустимые символы.";
        LogError(message);
        throw new ArgumentException(message, nameof(archiveName));
      }

      return normalizedName;
    }

    private static void ShowArchiveNotification(string title, string message, NotificationType notificationType)
    {
      NotificationHostService.Instance.Show(title, message, notificationType);
    }

    private static string GetUserFriendlySaveErrorMessage(Exception ex)
    {
      if (ex is InvalidOperationException invalidOperation &&
          invalidOperation.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
      {
        return "Файл с таким именем уже существует в выбранном архиве.";
      }

      if (ex is FileNotFoundException)
      {
        return "Выбранный архив не найден.";
      }

      if (ex is IOException ioException &&
          ioException.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase))
      {
        return "Архив сейчас занят другим процессом. Повторите попытку.";
      }

      return "Не удалось сохранить файл в архив.";
    }

    private static string GetUserFriendlyCreateArchiveErrorMessage(Exception ex)
    {
      if (ex is InvalidOperationException invalidOperation &&
          invalidOperation.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
      {
        return "Архив с таким именем уже существует. Выберите другое имя.";
      }

      if (ex is ArgumentException)
      {
        return "Имя архива содержит недопустимые символы.";
      }

      return "Не удалось создать архив.";
    }
  }
}
