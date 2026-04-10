using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Shared.DTO.TextEditor;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Static;
using Message;
using System.IO;
using System.Text;
using System.Windows;
using UI.Components.SearchControls;
using UI.Controls.TextEditorControl;
using static Ask.LogLib.LoggerUtility;

namespace UI.Services.FileManager
{
  /// <summary>
  /// Сервис для открытия файлов в текстовом редакторе.
  /// 
  /// Основные функции:
  /// <list type="bullet">
  ///   <item>Открывает указанный файл и загружает его содержимое в редактор.</item>
  ///   <item>Определяет кодировку и тип содержимого по расширению.</item>
  ///   <item>Создаёт новый редактор для файла или активирует уже открытый экземпляр.</item>
  ///   <item>Обрабатывает ошибки и уведомляет пользователя при сбоях.</item>
  /// </list>
  /// </summary>
  public class FileOpenService
  {
    private readonly UI.Components.MultiEditorMethods.FileManager _fileManager;

    public FileOpenService(UI.Components.MultiEditorMethods.FileManager fileManager)
    {
      _fileManager = fileManager;
    }

    /// <summary>
    /// Открывает файл по указанному пути и отображает его содержимое в текстовом редакторе.
    /// </summary>
    /// <param name="path">Полный путь к файлу.</param>
    public void OpenFile(string path)
    {
      Application.Current.Dispatcher.BeginInvoke(() =>
      {
        var fileName = ExtractFileName(path);
        if (!ValidateFileName(fileName, path))
          return;

        try
        {
          var (fileContent, encoding) = ReadFileContent(path);
          var container = EnsureTextEditorContainer();
          var fileType = DetermineFileType(fileName);

          if (TryActivateAlreadyOpenedFile(container, fileName, path))
            return;

          OpenNewFile(path, fileName, fileContent, encoding, fileType, container);
        }
        catch (Exception ex)
        {
          HandleFileReadError(ex, path);
        }
      });
    }

    #region 🔧 Основные шаги

    /// <summary>
    /// Проверяет корректность имени файла и показывает ошибку при необходимости.
    /// </summary>
    private bool ValidateFileName(string fileName, string path)
    {
      if (!string.IsNullOrEmpty(fileName))
        return true;

      MessageBoxCustom.Show("Ошибка при открытии файла", $"Ошибка при открытии файла {path}", image: MessageBoxImage.Error);
      return false;
    }

    /// <summary>
    /// Гарантирует наличие контейнера для текстового редактора.
    /// </summary>
    private TextEditorContainer EnsureTextEditorContainer()
    {
      return _fileManager.ContainerService.GetEditorContainer(EditorType.TextEditor)
             ?? _fileManager.ContainerService.CreateEditorContainer(EditorType.TextEditor);
    }

    /// <summary>
    /// Пытается активировать уже открытую вкладку с этим файлом.
    /// </summary>
    private bool TryActivateAlreadyOpenedFile(TextEditorContainer container, string fileName, string path)
    {
      if (!_fileManager.EditorWorkspaceModel.FilePaths.ContainsValue(path))
        return false;

      var existingItem = container.DockManager.DockItems.FirstOrDefault(item => item.TabText == fileName);
      if (existingItem == null)
        return false;

      existingItem.IsActiveDocument = true;
      _fileManager.DockItemService.ShowDockItem(container, existingItem);
      _fileManager.ControlManagerService.ShowEditorContainer(container, EditorType.TextEditor);
      return true;
    }

    /// <summary>
    /// Открывает новый файл в редакторе.
    /// </summary>
    private void OpenNewFile(string path, string fileName, string fileContent, Encoding encoding, FileType fileType, TextEditorContainer container)
    {
      var uniqueName = _fileManager.FileService.Name.EnsureUniqueFileName(path, fileName);
      var textEditorModel = new TextEditorModel(path, uniqueName, encoding);
      var textEditor = _fileManager.TextEditorService.CreateTextEditor(textEditorModel, fileContent, fileType);
      textEditor.ConfigureBreakpoints(interactive: false, visible: false);
      textEditor.TextArea.TextView.LineTransformers.Add(new BracesCommentColorizer());
      CancellationTokenSource redrawToken = null;

      textEditor.TextChanged += async (_, __) =>
      {
        redrawToken?.Cancel();
        redrawToken = new CancellationTokenSource();
        var token = redrawToken.Token;

        try
        {
          await Task.Delay(80, token); // ждём, пока пользователь закончит ввод
          if (!token.IsCancellationRequested)
          {
            // безопасный вызов из UI-потока
            Application.Current.Dispatcher.Invoke(() =>
            {
              textEditor.TextArea.TextView.Redraw();
            });
          }
        }
        catch (TaskCanceledException)
        {
          // просто игнорируем отменённую задержку
        }
      };


      if (fileType == FileType.Protocol)
        textEditor.IsReadOnly = true;

      EditorEventAdapter.RaiseTextEditorActivated(textEditor);
      _fileManager.DockItemService.ShowEditorDockItem(uniqueName, container, textEditor);
      _fileManager.ControlManagerService.ShowEditorContainer(container, EditorType.TextEditor);
    }

    /// <summary>
    /// Обрабатывает ошибку при чтении файла.
    /// </summary>
    private void HandleFileReadError(Exception ex, string path)
    {
      MessageBoxCustom.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка", image: MessageBoxImage.Error);
      LogException($"Ошибка при чтении файла {path}", ex);
    }

    #endregion

    #region 📂 Работа с содержимым файла

    /// <summary>
    /// Считывает содержимое указанного файла и определяет его кодировку.
    /// </summary>
    internal (string, Encoding) ReadFileContent(string path)
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
      //var encoding = EncodingService.DetectEncodingFromFile(path);
      var extention = Path.GetExtension(path).ToLowerInvariant();
      Encoding encoding;
      if (extention == ".pkw" || extention == ".txt" || extention == ".lstw" || extention == ".lst")
      {
        encoding = Encoding.UTF8;
      }
      else
      {
        encoding = Encoding.GetEncoding(866);
      }

      var lines = File.ReadLines(path, encoding)
                      .Where(line => !string.IsNullOrEmpty(line))
                      .ToList();

      var content = string.Join("\n", lines);
      return (content, encoding);
    }

    /// <summary>
    /// Определяет тип файла по его расширению.
    /// </summary>
    public FileType DetermineFileType(string fileName)
    {
      if (string.IsNullOrEmpty(fileName))
        return FileType.None;

      return Path.GetExtension(fileName).ToLowerInvariant() switch
      {
        ".pk" => FileType.PK,
        ".pkw" => FileType.PKW,
        ".acs" => FileType.PK,
        ".opk" => FileType.OPK,
        ".opkw" => FileType.OPKW,
        ".lst" => FileType.Protocol,
        ".lstw" => FileType.Protocol,
        _ => FileType.None
      };
    }

    /// <summary>
    /// Извлекает имя файла из полного пути.
    /// </summary>
    private string ExtractFileName(string path)
    {
      return string.IsNullOrEmpty(path) ? string.Empty : Path.GetFileName(path);
    }

    #endregion
  }
}
