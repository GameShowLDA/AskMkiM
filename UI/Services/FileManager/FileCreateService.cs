using Ask.Core.Shared.DTO.TextEditor;
using Ask.Core.Shared.Metadata.Static;
using System.Windows;
using UI.Components.SearchControls;
using UI.Controls.TextEditor;

namespace UI.Services.FileManager
{
  /// <summary>
  /// Сервис для создания новых файлов и их регистрации в рабочем пространстве редактора.
  /// </summary>
  public class FileCreateService
  {
    private readonly UI.Components.MultiEditorMethods.FileManager _fileManager;

    /// <summary>
    /// Инициализирует новый экземпляр сервиса создания файлов.
    /// </summary>
    /// <param name="fileManager">Экземпляр основного файлового менеджера, через который выполняются операции создания и регистрации файлов.</param>
    public FileCreateService(UI.Components.MultiEditorMethods.FileManager fileManager)
    {
      _fileManager = fileManager;
    }

    /// <summary>
    /// Создаёт новый пустой файл и добавляет его в рабочее пространство редактора.
    /// 
    /// Алгоритм:
    /// <list type="number">
    ///   <item>Проверяет и при необходимости создаёт контейнер текстового редактора.</item>
    ///   <item>Генерирует уникальное имя файла.</item>
    ///   <item>Создаёт модель файла и сам редактор.</item>
    ///   <item>Отображает редактор в UI и регистрирует файл в системе путей.</item>
    /// </list>
    /// </summary>
    public void CreateNewFile()
    {
      var container = EnsureTextEditorContainer();
      var fileName = GenerateUniqueFileName();
      var editor = CreateTextEditor(fileName);

      ShowEditorInUI(container, fileName, editor);
      RegisterFile(fileName);
    }

    #region 🔧 Подметоды (SRP)

    /// <summary>
    /// Проверяет наличие контейнера текстового редактора и создаёт его при необходимости.
    /// </summary>
    private TextEditorContainer EnsureTextEditorContainer()
    {
      return _fileManager.ContainerService.GetEditorContainer(EditorType.TextEditor)
             ?? _fileManager.ContainerService.CreateEditorContainer(EditorType.TextEditor);
    }

    /// <summary>
    /// Генерирует уникальное имя для нового файла (например: "Новый", "Новый1", "Новый2" и т.д.).
    /// </summary>
    private string GenerateUniqueFileName()
    {
      var baseName = "Новый";
      var controlName = baseName;
      var counter = 0;

      while (_fileManager.EditorWorkspaceModel.FilePaths.ContainsKey(controlName))
      {
        counter++;
        controlName = baseName + counter;
      }

      return controlName;
    }

    /// <summary>
    /// Создаёт новый экземпляр текстового редактора для указанного файла.
    /// </summary>
    private TextEditorUI CreateTextEditor(string fileName)
    {
      var textEditorModel = new TextEditorModel(string.Empty, fileName);
      textEditorModel.SavedTextSnapshot = string.Empty;
      var textEditor = new TextEditorUI
      {
        TextEditorModel = textEditorModel
      };
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


      return textEditor;
    }

    /// <summary>
    /// Отображает редактор в UI в указанном контейнере.
    /// </summary>
    private void ShowEditorInUI(TextEditorContainer container, string fileName, TextEditorUI textEditor)
    {
      _fileManager.DockItemService.ShowEditorDockItem(fileName, container, textEditor);
    }

    /// <summary>
    /// Добавляет новый файл в систему путей рабочего пространства.
    /// </summary>
    private void RegisterFile(string fileName)
    {
      _fileManager.EditorWorkspaceModel.FilePaths.Add(fileName, string.Empty);
    }

    #endregion
  }
}
