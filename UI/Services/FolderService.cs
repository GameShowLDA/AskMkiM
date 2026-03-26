using Ask.Core.Shared.Metadata.Static;
using System.Diagnostics;
using System.IO;
using UI.Controls;
using UI.Controls.TextEditorControl;

namespace UI.Services
{
  /// <summary>
  /// Сервис для работы с файловой системой, связанной с редактором.
  /// 
  /// Основные задачи:
  /// <list type="bullet">
  ///   <item>Открытие папки, содержащей текущий открытый файл.</item>
  ///   <item>Поддержка как обычного текстового редактора, так и режима трансляции.</item>
  ///   <item>Интеграция с проводником ОС для быстрого перехода к файлу.</item>
  /// </list>
  /// </summary>
  public class FolderService
  {
    private readonly UI.Components.MultiEditorMethods.FileManager _fileManager;

    /// <summary>
    /// Инициализирует новый экземпляр сервиса работы с папками.
    /// </summary>
    /// <param name="fileManager">Главный файловый менеджер, предоставляющий доступ к контейнерам редактора.</param>
    public FolderService(UI.Components.MultiEditorMethods.FileManager fileManager)
    {
      _fileManager = fileManager;
    }

    /// <summary>
    /// Открывает в проводнике операционной системы папку, в которой находится файл, открытый в текущем редакторе.
    /// Поддерживает два режима:
    /// <list type="bullet">
    ///   <item>Если открыт стандартный текстовый редактор — открывает папку активного файла.</item>
    ///   <item>Если активен транслятор — открывает папку исходного (левого) файла.</item>
    /// </list>
    /// </summary>
    public void OpenActiveFileFolder()
    {
      TextEditorContainer textEditorContainer = _fileManager.ContainerService.GetEditorContainer(EditorType.TextEditor);
      if (textEditorContainer == null)
      {
        textEditorContainer = _fileManager.ContainerService.GetEditorContainer(EditorType.Translator);
        if (textEditorContainer == null)
        {
          return;
        }
        else
        {
          var translatorEditor = textEditorContainer.DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem == true);
          if (translatorEditor != null && translatorEditor.Content is TranslatorItem translator)
          {
            var leftEditor = translator.GetLeftBox();
            OpenFileFolder(leftEditor.GetTextEditor().TextEditorModel.FilePath);
          }
        }
      }
      else
      {
        var activeTextEditor = textEditorContainer.GetTextEditor();
        if (activeTextEditor != null)
        {
          OpenFileFolder(activeTextEditor.TextEditorModel.FilePath);
        }
      }
    }

    /// <summary>
    /// Открывает в проводнике операционной системы папку, содержащую указанный файл.
    /// </summary>
    /// <param name="path">Полный путь к файлу.</param>
    public static void OpenFileFolder(string path)
    {
      if (string.IsNullOrWhiteSpace(path))
      {
        return;
      }

      string fullPath;
      try
      {
        fullPath = Path.GetFullPath(path);
      }
      catch
      {
        fullPath = path;
      }

      if (File.Exists(fullPath))
      {
        Process.Start(new ProcessStartInfo
        {
          FileName = "explorer.exe",
          Arguments = $"/select,\"{fullPath}\"",
          UseShellExecute = true
        });
        return;
      }

      string folder = Directory.Exists(fullPath)
        ? fullPath
        : Path.GetDirectoryName(fullPath);

      if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
      {
        Process.Start(new ProcessStartInfo
        {
          FileName = folder,
          UseShellExecute = true,
          Verb = "open"
        });
      }
    }
  }
}
