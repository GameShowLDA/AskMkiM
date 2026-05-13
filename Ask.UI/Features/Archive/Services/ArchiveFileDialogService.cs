using System.Windows;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using OpenFolderDialog = Microsoft.Win32.OpenFolderDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace Ask.UI.Features.Archive.Services
{
  public static class ArchiveFileDialogService
  {
    public static string? SelectFileToAddToArchive(FrameworkElement ownerElement)
    {
      var dialog = new OpenFileDialog
      {
        Title = "Выберите файл для добавления",
        Filter = "OPKW file (*.opkw)|*.opkw",
        CheckFileExists = true,
        Multiselect = false,
      };

      return ShowDialog(ownerElement, dialog)
        ? dialog.FileName
        : null;
    }

    public static string? SelectArchiveExportPath(FrameworkElement ownerElement, string suggestedFileName)
    {
      var dialog = new SaveFileDialog
      {
        Title = "Сохранить архив на диск",
        Filter = "Архив ASK (*.apkw)|*.apkw",
        DefaultExt = ".apkw",
        AddExtension = true,
        FileName = suggestedFileName,
        OverwritePrompt = true,
      };

      return ShowDialog(ownerElement, dialog)
        ? dialog.FileName
        : null;
    }

    public static string? SelectPkwExportPath(FrameworkElement ownerElement, string suggestedFileName)
    {
      var dialog = new SaveFileDialog
      {
        Title = "Save as PKW",
        Filter = "PKW file (*.pkw)|*.pkw",
        DefaultExt = ".pkw",
        AddExtension = true,
        FileName = suggestedFileName,
        OverwritePrompt = true,
      };

      return ShowDialog(ownerElement, dialog)
        ? dialog.FileName
        : null;
    }

    public static string? SelectArchiveImportFile(FrameworkElement? ownerElement)
    {
      var dialog = new OpenFileDialog
      {
        Title = "Загрузить архив",
        Filter = "Архив ASK (*.apkw)|*.apkw",
        CheckFileExists = true,
        Multiselect = false,
      };

      return ShowDialog(ownerElement, dialog)
        ? dialog.FileName
        : null;
    }

    public static string? SelectFolder(FrameworkElement? ownerElement, string title)
    {
      var dialog = new OpenFolderDialog
      {
        Title = title,
        Multiselect = false,
        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
      };

      return ShowDialog(ownerElement, dialog)
        ? dialog.FolderName
        : null;
    }

    private static bool ShowDialog(FrameworkElement? ownerElement, Microsoft.Win32.CommonDialog dialog)
    {
      var owner = ownerElement != null
        ? Window.GetWindow(ownerElement)
        : Application.Current?.MainWindow;

      return owner != null
        ? dialog.ShowDialog(owner) == true
        : dialog.ShowDialog() == true;
    }
  }
}
