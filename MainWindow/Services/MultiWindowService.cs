using System.Windows.Controls;
using UI.Components;
using UI.Controls;
using UI.Controls.Runner;
using UI.Controls.TextEditor;
using static UI.Components.Invoke.OpenFileButton;

namespace MainWindowProgram.Services
{
  /// <summary>
  /// Реализация сервиса для управления компонентом <see cref="MultiWindowControl"/>.
  /// Позволяет добавлять пользовательские элементы управления в различные окна интерфейса.
  /// </summary>
  public class MultiWindowService
  {
    /// <summary>
    /// Компонент, управляющий отображением множества окон и вкладок.
    /// </summary>
    private readonly MultiWindowControl _multiWindowControl;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MultiWindowService"/>.
    /// </summary>
    /// <param name="multiWindowControl">Контейнер окон и вкладок, с которым работает сервис.</param>
    public MultiWindowService(MultiWindowControl multiWindowControl)
    {
      _multiWindowControl = multiWindowControl;
    }

    /// <summary>
    /// Асинхронно добавляет элемент управления в редактор.
    /// </summary>
    /// <param name="name">Название вкладки или окна.</param>
    /// <param name="control">Элемент управления, который необходимо отобразить.</param>
    /// <param name="type">Тип окна, в котором должен отображаться элемент управления.</param>
    /// <returns>Задача, представляющая операцию добавления.</returns>
    public Task AddControlAsync(string name, UserControl control, TypeWindow type)
    {
      _multiWindowControl.AddControl(name, control, type);
      return Task.CompletedTask;
    }

    /// <summary>
    /// Добавляет новый MultiEditorControl в контейнер.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    public Task OpenFileInEditor(string filePath)
    {
      _multiWindowControl.OpenFileInEditor(filePath);
      return Task.CompletedTask;
    }

    /// <summary>
    /// Добавляет новый MultiEditorControl в контейнер.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    public Task ViewProtocol(string filePath)
    {
      _multiWindowControl.ViewProtocol(filePath);
      return Task.CompletedTask;
    }

    /// <summary>
    /// Добавляет новый MultiEditorControl в контейнер.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    public Task OpenFileFromEvent(string filePath)
    {
      _multiWindowControl.OpenFileInEditor(filePath);
      return Task.CompletedTask;
    }

    /// <summary>
    /// Создает новый файл в редакторе.
    /// </summary>
    /// <remarks>
    /// Этот метод вызывает создание нового файла в редакторе, если редактор был инициализирован.
    /// Если редактор не инициализирован, выводится сообщение об ошибке.
    /// </remarks>
    public Task CreateNewFile()
    {
      _multiWindowControl.CreateNewFile();
      return Task.CompletedTask;
    }

    /// <summary>
    /// Сохраняет файл.
    /// </summary>
    /// <returns>Асинхронную задачу, представляющую результат выполнения.</returns>
    public Task SaveFile()
    {
      _multiWindowControl.SaveFile();
      return Task.CompletedTask;
    }

    /// <summary>
    /// Сохранить файл как.
    /// </summary>
    /// <returns>Асинхронную задачу, представляющую результат выполнения.</returns>
    public Task SaveFileAs()
    {
      _multiWindowControl.SaveFileAs();
      return Task.CompletedTask;
    }

    /// <summary>
    /// Выводит файл на печать.
    /// </summary>
    /// <returns>Асинхронную задачу, представляющую результат выполнения.</returns>
    public Task PrintFile()
    {
      _multiWindowControl.PrintFile();
      return Task.CompletedTask;
    }

    /// <summary>
    /// Получает активный текстовый редактор.
    /// </summary>
    /// <returns>Асинхронную задачу, представляющую результат поиска текстового редактора.</returns>
    public Task<TextEditorUI> GetActiveTextEditor(EditorType editorType)
    {
      var foundEditor = _multiWindowControl.GetActiveTextEditor(editorType);
      return Task.FromResult(foundEditor);
    }

    public Task<TextEditorUI> GetActiveTextEditor()
    {
      var foundEditor = _multiWindowControl.GetActiveTextEditor();
      return Task.FromResult(foundEditor);
    }

    /// <summary>
    /// Закрывает вкладку с активным текстовым редактором.
    /// </summary>
    /// <param name="isTranslation">Переменная, показывающая, выполняется закрытие вкладки при трансляции или нет.</param>
    /// <returns>Возвращает <c>true</c>, если вкладка была закрыта, <c>false</c> в противном случае.</returns>  
    public bool RemoveActiveTextEditor(bool isTranslation)
    {
      return _multiWindowControl.RemoveActiveTextEditor(isTranslation);
    }

    /// <summary>
    /// Удаляет контрол.
    /// </summary>
    /// <param name="editorType">Тип вкладки.</param>
    public void RemoveControl(EditorType editorType)
    {
      _multiWindowControl.RemoveControl(editorType);
    }

    /// <summary>
    /// Создает файл трансляции.
    /// </summary>
    /// <returns>Текстовый редактор с файлом трансляции.</returns>
    public TextEditorUI CreateTranslationFileAsync()
    {
      return _multiWindowControl.CreateTranslationFileAsync();
    }

    /// <summary>
    /// Получает активный контейнер с вкладками.
    /// </summary>
    /// <param name="editorType">Тип вкладок.</param>
    /// <returns>Асинхронную задачу, представляющую результат поиска контейнера.</returns>
    internal Task<TextEditorContainer> GetActiveTextEditorContainer(EditorType editorType)
    {
      var foundContainer = _multiWindowControl.GetActiveTextEditorContainer(editorType);
      return Task.FromResult(foundContainer);
    }


    internal async Task DeleteTranslatorItem(TranslatorItem translatorItem, EditorType editorType)
    {
     await _multiWindowControl.DeleteTranslatorItem(translatorItem, editorType);
    }


 /// <summary>
    /// Добавляет вкладку с транслятором.
    /// </summary>
    /// <param name="editor">Текстовый редактор с файлом, который необходимо транслировать.</param>
    /// <param name="translateEditor">Текстовый редактор с странслированным файлом.</param>
    /// <param name="editorType">Тип вкладки.</param>
    /// <returns>Асинхронную задачу, представляющую результат выполнения.</returns>
    internal Task<TranslatorItem> AddTranslatorItem(TextEditorUI editor, TextEditorUI translateEditor, EditorType editorType)
    {
      return _multiWindowControl.AddTranslatorItem(editor, translateEditor, editorType);
    }

    internal Task AddRunItem(RunControl runControl, EditorType editorType)
    {
      return _multiWindowControl.AddRunItem(runControl, editorType);
    }

  /// <summary>
    /// Открывает папку, содержащую файл, в проводнике.
    /// </summary>
    /// <returns>Асинхронную задачу, представляющую результат выполнения.</returns>
    internal Task OpenFolder()
    {
      _multiWindowControl.OpenFolder();
      return Task.CompletedTask;
    }

    public async Task OpenArchiveAsync()
    {
      await _multiWindowControl.OpenArchiveAsync();
    }

    internal async Task CloseRunItem(RunControl runControl, EditorType editorType)
    {
      await _multiWindowControl.CloseRunItem(runControl, editorType);
    }
  }
}
