using Ask.Core.Shared.DTO.Protocol;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Shared.Metadata.View.EditorHost;
using Ask.Core.Shared.Metadata.View.EditorHost.TextEditor;
using System.Windows.Controls;
using System.Windows.Forms.Design;
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
    /// Добавляет новый MultiEditorControl в контейнер.
    /// </summary>
    /// <param name="filePath">Путь к файлу.</param>
    public void OpenFileFromEvent(string filePath) => EditorDocumentService.OpenFile(filePath);

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
