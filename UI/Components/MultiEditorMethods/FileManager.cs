using Ask.Core.Shared.Metadata.View.EditorHost;
using UI.Controls.TextEditor;
using UI.Services;
using UI.Services.FileManager;
using UI.Services.ProtocolManager;
using UI.Services.Services;

namespace UI.Components.MultiEditorMethods
{
  /// <summary>
  /// Главный менеджер для работы с файлами и текстовыми редакторами в приложении.  
  /// Отвечает за инициализацию и связывание всех файловых сервисов, необходимых для функционирования редактора.
  /// Основные задачи:
  /// <list type="bullet">
  ///   <item>Инициализация всех сервисов, отвечающих за работу с файлами, вкладками, контейнерами и сессиями.</item>
  ///   <item>Предоставление централизованного доступа к сервисам через единый объект.</item>
  ///   <item>Хранение и управление состоянием рабочего пространства редактора.</item>
  /// </list>
  /// </summary>
  public class FileManager
  {
    /// <summary>
    /// Инициализирует новый экземпляр <see cref="FileManager"/> и создаёт все связанные сервисы.
    /// </summary>
    /// <param name="multiEditorControl">Экземпляр <see cref="MultiEditorControl"/>, представляющий основное рабочее пространство редактора.</param>
    public FileManager(MultiEditorControl multiEditorControl)
    {
      EditorWorkspaceModel = new EditorWorkspaceModel(multiEditorControl);
      ContainerService = new ContainerService(this);
      ProtocolService = new ProtocolService(this);
      ControlManagerService = new ControlManagerService(EditorWorkspaceModel);
      DockItemService = new DockItemService(this);
      FolderService = new FolderService(this);
      RunControlService = new RunControlService(this);
      TextEditorService = new TextEditorService(this);
      TranslationService = new TranslationService(this);
      FileService = new FileService(this);
    }

    public IEditorDocumentService EditorDocumentService => FileService;

    /// <summary>
    /// Модель рабочего пространства редактора, содержащая информацию о состоянии вкладок, контейнеров, путей и активных редакторов.
    /// </summary>
    public EditorWorkspaceModel EditorWorkspaceModel;

    /// <summary>
    /// Сервис управления файловыми операциями: открытие, сохранение, создание, сравнение и работа с именами файлов.
    /// </summary>
    public FileService FileService;

    /// <summary>
    /// Сервис управления контейнерами вкладок редактора: создание, получение и удаление контейнеров.
    /// </summary>
    public ContainerService ContainerService;

    /// <summary>
    /// Сервис формирования и отображения протоколов испытаний, включая экспорт в PDF и открытие в редакторе.
    /// </summary>
    public ProtocolService ProtocolService;

    /// <summary>
    /// Сервис управления отображением контейнеров и вкладок в пользовательском интерфейсе.
    /// </summary>
    public ControlManagerService ControlManagerService;

    /// <summary>
    /// Сервис управления вкладками (DockItem): создание, показ, закрытие и обработка событий вкладок редактора.
    /// </summary>
    public DockItemService DockItemService;

    /// <summary>
    /// Сервис для работы с файловой структурой и директориями (создание папок, навигация и т.д.).
    /// </summary>
    public FolderService FolderService;

    /// <summary>
    /// Сервис для работы с панелью запуска тестов и управлением запуском сценариев.
    /// </summary>
    public RunControlService RunControlService;

    /// <summary>
    /// Сервис для создания и управления экземплярами <see cref="TextEditorUI"/> в приложении.
    /// </summary>
    public TextEditorService TextEditorService;

    /// <summary>
    /// Сервис для создания и управления трансляцией текстовых файлов, включая отображение результатов трансляции.
    /// </summary>
    public TranslationService TranslationService;
  }
}
