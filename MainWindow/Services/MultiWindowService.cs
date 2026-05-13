using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Shared.Metadata.View.EditorHost;
using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;
using Ask.Core.Shared.DTO.TextEditor;
using System.Windows.Controls;
using UI.Components;
using UI.Controls;
using UI.Controls.TextEditorControl;
using UserControl = System.Windows.Controls.UserControl;

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
    /// Подсистема выполнения: управляет вкладками запуска и отображением результатов выполнения в редакторном хосте.
    /// </summary>
    public readonly IRunService RunService;

    /// <summary>
    /// Подсистема документов: обеспечивает операции создания, открытия, сохранения и печати документов редактора.
    /// </summary>
    public readonly IEditorDocumentService EditorDocumentService;
    public readonly IProtocolViewerService ProtocolViewerService;
    public readonly IWorkspaceService WorkspaceService;
    public readonly ITranslationService TranslationService;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MultiWindowService"/>.
    /// </summary>
    /// <param name="multiWindowControl">Контейнер окон и вкладок, с которым работает сервис.</param>
    public MultiWindowService(
      MultiWindowControl multiWindowControl,
      IRunService runService,
      IEditorDocumentService editorDocumentService,
      IProtocolViewerService protocolViewerService,
      IWorkspaceService workspaceService,
      ITranslationService translationService)
    {
      _multiWindowControl = multiWindowControl;
      RunService = runService;
      EditorDocumentService = editorDocumentService;
      ProtocolViewerService = protocolViewerService;
      WorkspaceService = workspaceService;
      TranslationService = translationService;
    }

    /// <summary>
    /// Открывает файл в редакторе по указанному пути.
    /// Используется, как правило, при обработке внешних событий (например, выбора файла).
    /// </summary>
    /// <param name="filePath">Полный путь к открываемому файлу.</param>
    public void OpenFileFromEvent(string filePath) => EditorDocumentService.OpenFile(filePath);

    /// <summary>
    /// Возвращает активный текстовый редактор указанного типа.
    /// </summary>
    /// <param name="editorType">Тип редактора (например, основной или транслятор).</param>
    /// <returns>
    /// Экземпляр <see cref="TextEditorUI"/>, представляющий активный редактор,
    /// либо <c>null</c>, если редактор отсутствует.
    /// </returns>
    public TextEditorUI GetActiveTextEditor(EditorType editorType) => _multiWindowControl.GetActiveTextEditor(editorType);

    /// <summary>
    /// Возвращает текущий активный текстовый редактор независимо от типа.
    /// </summary>
    /// <returns>
    /// Экземпляр <see cref="TextEditorUI"/>, представляющий активный редактор,
    /// либо <c>null</c>, если ни один редактор не активен.
    /// </returns>
    public TextEditorUI GetActiveTextEditor() => _multiWindowControl.GetActiveTextEditor();

    /// <summary>
    /// Возвращает список файлов, открытых во вкладках основного текстового редактора.
    /// </summary>
    public IReadOnlyList<OpenTextEditorDescriptor> GetOpenTextEditors() => _multiWindowControl.GetOpenTextEditors();

    /// <summary>
    /// Возвращает активный пользовательский элемент управления в рабочей области.
    /// </summary>
    /// <returns>
    /// Текущий активный <see cref="UserControl"/> или <c>null</c>, если рабочая область пуста.
    /// </returns>
    public UserControl? GetActiveWorkspaceControl() => _multiWindowControl.GetActiveWorkspaceControl();

    /// <summary>
    /// Закрывает вкладку с активным текстовым редактором.
    /// </summary>
    /// <param name="isTranslation">
    /// Указывает, связано ли закрытие вкладки с процессом трансляции.
    /// Может влиять на дополнительную логику очистки или сохранения.
    /// </param>
    /// <returns>
    /// <c>true</c>, если вкладка была успешно закрыта;
    /// <c>false</c>, если закрытие не выполнено (например, нет активного редактора).
    /// </returns>
    public bool RemoveActiveTextEditor(bool isTranslation)
    {
      return _multiWindowControl.RemoveActiveTextEditor(isTranslation);
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
    internal Task<TranslatorItem> AddTranslatorItem(ITextEditorView editor, ITextEditorView translateEditor, EditorType editorType)
    {
      return _multiWindowControl.AddTranslatorItem(editor, translateEditor, editorType);
    }
  }
}
