using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Shared.Metadata.View.EditorHost;
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
    private readonly IRunService _runService;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MultiWindowService"/>.
    /// </summary>
    /// <param name="multiWindowControl">Контейнер окон и вкладок, с которым работает сервис.</param>
    public MultiWindowService(MultiWindowControl multiWindowControl, IRunService runService)
    {
      _multiWindowControl = multiWindowControl;
      _runService = runService;
    }

    /// <summary>
    /// Асинхронно добавляет элемент управления в редактор.
    /// </summary>
    /// <param name="name">Название вкладки или окна.</param>
    /// <param name="control">Элемент управления, который необходимо отобразить.</param>
    /// <param name="type">Тип окна, в котором должен отображаться элемент управления.</param>
    public void AddControl(string name, UserControl control, TypeWindow type) => _multiWindowControl.AddControl(name, control, type);

    /// <summary>
    /// Добавляет новый MultiEditorControl в контейнер.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    public void OpenFileInEditor(string filePath) => _multiWindowControl.OpenFileInEditor(filePath);

    /// <summary>
    /// Добавляет новый MultiEditorControl в контейнер.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    public void ViewProtocol(ProtocolModel protocol, bool showInSoftware) => _multiWindowControl.ViewProtocol(protocol, showInSoftware);

    /// <summary>
    /// Добавляет новый MultiEditorControl в контейнер.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    public void OpenFileFromEvent(string filePath) => _multiWindowControl.OpenFileInEditor(filePath);

    /// <summary>
    /// Создает новый файл в редакторе.
    /// </summary>
    /// <remarks>
    /// Этот метод вызывает создание нового файла в редакторе, если редактор был инициализирован.
    /// Если редактор не инициализирован, выводится сообщение об ошибке.
    /// </remarks>
    public void CreateNewFile() => _multiWindowControl.CreateNewFile();

    /// <summary>
    /// Сохраняет файл.
    /// </summary>
    public void SaveFile() => _multiWindowControl.SaveFile();

    /// <summary>
    /// Сохранить файл как.
    /// </summary>
    public void SaveFileAs() => _multiWindowControl.SaveFileAs();

    /// <summary>
    /// Выводит файл на печать.
    /// </summary>
    public void PrintFile() => _multiWindowControl.PrintFile();

    /// <summary>
    /// Получает активный текстовый редактор.
    /// </summary>
    /// <returns>Асинхронную задачу, представляющую результат поиска текстового редактора.</returns>
    public TextEditorUI GetActiveTextEditor(EditorType editorType) => _multiWindowControl.GetActiveTextEditor(editorType);

    public TextEditorUI GetActiveTextEditor() => _multiWindowControl.GetActiveTextEditor();

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
    public TextEditorUI CreateTranslationFileAsync(string parentFilePath)
    {
      return _multiWindowControl.CreateTranslationFileAsync(parentFilePath);
    }

    /// <summary>
    /// Получает активный контейнер с вкладками.
    /// </summary>
    /// <param name="editorType">Тип вкладок.</param>
    /// <returns>Асинхронную задачу, представляющую результат поиска контейнера.</returns>
    internal TextEditorContainer GetActiveTextEditorContainer(EditorType editorType) => _multiWindowControl.GetActiveTextEditorContainer(editorType);

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

    internal Task AddRunItem(IRunView runControl, EditorType editorType)
    {
      return _multiWindowControl.AddRunItem(runControl, editorType);
    }

    /// <summary>
    /// Открывает папку, содержащую файл, в проводнике.
    /// </summary>
    internal void OpenFolder() => _multiWindowControl.OpenFolder();

    internal async Task CloseRunItem(IRunView runControl, EditorType editorType)
    {
      await _multiWindowControl.CloseRunItem(runControl, editorType);
    }
  }
}
