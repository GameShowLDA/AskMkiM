using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using static Ask.LogLib.LoggerUtility;

namespace UI.Services
{
  internal sealed class TranslatedFileSaveService
  {
    private const string DefaultFileNameWithoutExtension = "translated";
    private const string OpkwExtension = ".opkw";

    public bool SaveToDisk(FrameworkElement ownerElement, string translatedText, string? sourceFilePath)
    {
      if (ownerElement == null)
      {
        throw new ArgumentNullException(nameof(ownerElement));
      }

      if (string.IsNullOrWhiteSpace(translatedText))
      {
        ShowNotification(
          "Сохранение на диск",
          "Нет данных для сохранения на диск.",
          NotificationType.Warning);
        return false;
      }

      var suggestedFileName = GetSuggestedFileName(sourceFilePath);
      var saveFileDialog = CreateSaveFileDialog(suggestedFileName);
      var ownerWindow = Window.GetWindow(ownerElement);
      var dialogResult = ownerWindow != null
        ? saveFileDialog.ShowDialog(ownerWindow)
        : saveFileDialog.ShowDialog();

      if (dialogResult != true)
      {
        return false;
      }

      var filePath = EnsureOpkwExtension(saveFileDialog.FileName);
      try
      {
        File.WriteAllText(filePath, translatedText, Encoding.UTF8);
        LogInformation($"Файл {filePath} сохранен на диск.");
        ShowNotification(
          "Сохранение на диск",
          $"Файл {Path.GetFileName(filePath)} сохранён на диск.",
          NotificationType.Success);
        return true;
      }
      catch (Exception ex)
      {
        LogException(ex, $"Ошибка при сохранении файла: {filePath}");
        ShowNotification(
          "Сохранение на диск",
          ex.Message,
          NotificationType.Error);
        return false;
      }
    }

    private static SaveFileDialog CreateSaveFileDialog(string suggestedFileNameWithoutExtension)
    {
      var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(suggestedFileNameWithoutExtension);
      if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
      {
        fileNameWithoutExtension = DefaultFileNameWithoutExtension;
      }

      return new SaveFileDialog
      {
        Filter = "Файлы программ контроля (*.opkw)|*.opkw",
        Title = "Сохранить файл на диск",
        FileName = $"{fileNameWithoutExtension}{OpkwExtension}",
        DefaultExt = OpkwExtension,
        AddExtension = true,
        OverwritePrompt = true,
      };
    }

    private static string EnsureOpkwExtension(string? filePath)
    {
      if (string.IsNullOrWhiteSpace(filePath))
      {
        return string.Empty;
      }

      return string.Equals(Path.GetExtension(filePath), OpkwExtension, StringComparison.OrdinalIgnoreCase)
        ? filePath
        : Path.ChangeExtension(filePath, OpkwExtension);
    }

    private static string GetSuggestedFileName(string? sourceFilePath)
    {
      var fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
      return string.IsNullOrWhiteSpace(fileName)
        ? DefaultFileNameWithoutExtension
        : fileName;
    }

    private static void ShowNotification(string title, string message, NotificationType notificationType)
    {
      NotificationHostService.Instance.Show(title, message, notificationType);
    }
  }
}
