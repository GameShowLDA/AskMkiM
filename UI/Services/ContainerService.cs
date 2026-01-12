using Ask.Core.Shared.Metadata.Static;
using System.Windows.Controls;
using UI.Components.Invoke;
using UI.Components.MultiEditorMethods;
using UI.Controls.TextEditor;
using static Ask.LogLib.LoggerUtility;

namespace UI.Services
{
  /// <summary>
  /// Сервис управления контейнерами вкладок текстового редактора.
  /// 
  /// Основные задачи:
  /// <list type="bullet">
  ///   <item>Создание новых контейнеров для заданного типа редакторов.</item>
  ///   <item>Получение существующих контейнеров по их типу.</item>
  ///   <item>Добавление и удаление контейнеров из менеджера вкладок.</item>
  /// </list>
  /// </summary>
  public class ContainerService
  {
    private readonly UI.Components.MultiEditorMethods.FileManager _fileManager;

    /// <summary>
    /// Инициализирует новый экземпляр сервиса управления контейнерами.
    /// </summary>
    public ContainerService(UI.Components.MultiEditorMethods.FileManager fileManager)
    {
      _fileManager = fileManager;
    }

    #region 📁 Создание контейнера

    /// <summary>
    /// Создаёт новый контейнер для вкладок указанного типа редактора и добавляет его в менеджер интерфейса.
    /// </summary>
    /// <param name="editorType">Тип редактора, для которого создаётся контейнер.</param>
    /// <param name="fileType">Тип окна, в котором будет открыт контейнер (по умолчанию — <see cref="OpenFileButton.TypeWindow.Files"/>).</param>
    /// <returns>Созданный экземпляр <see cref="TextEditorContainer"/>.</returns>
    public TextEditorContainer CreateEditorContainer(
        EditorType editorType,
        OpenFileButton.TypeWindow fileType = OpenFileButton.TypeWindow.Files)
    {
      var container = new TextEditorContainer();
      AddContainerToManager(editorType.ToString(), container, fileType);
      return container;
    }

    #endregion

    #region 🔍 Получение контейнера

    /// <summary>
    /// Получает существующий контейнер для заданного типа редактора.
    /// </summary>
    /// <param name="editorType">Тип редактора.</param>
    /// <returns>Контейнер, если он существует, иначе <c>null</c>.</returns>
    public TextEditorContainer GetEditorContainer(EditorType editorType)
    {
      var page = FindPageByEditorType(editorType);
      return page == null ? null : ExtractContainerFromPage(page);
    }

    /// <summary>
    /// Находит страницу контейнера по типу редактора.
    /// </summary>
    private OpenFileButton FindPageByEditorType(EditorType editorType)
    {
      return _fileManager.EditorWorkspaceModel.OpenPages
          .FirstOrDefault(page => page.Text == editorType.DisplayName);
    }

    /// <summary>
    /// Извлекает контейнер из найденной страницы, если он существует.
    /// </summary>
    private TextEditorContainer ExtractContainerFromPage(OpenFileButton page)
    {
      int index = _fileManager.EditorWorkspaceModel.OpenPages.IndexOf(page);
      if (index < 0 || index >= _fileManager.EditorWorkspaceModel.UserControls.Count)
        return null;

      return _fileManager.EditorWorkspaceModel.UserControls[index] as TextEditorContainer;
    }

    #endregion

    #region 🗑️ Удаление контейнера

    /// <summary>
    /// Удаляет указанный контейнер редактора из интерфейса и менеджера вкладок.
    /// </summary>
    public void RemoveEditorContainer(TextEditorContainer container, EditorType editorType)
    {
      var controlManager = CreateControlManager();
      var page = _fileManager.EditorWorkspaceModel.OpenPages
          .FirstOrDefault(p => p.Text == editorType.ToString());

      if (page != null)
      {
        controlManager.RemoveControl(page, container);
        LogDebug($"Контейнер для {editorType} удалён из интерфейса.");
      }
    }

    #endregion

    #region ➕ Добавление контейнера

    /// <summary>
    /// Добавляет контейнер вкладок в менеджер интерфейса.
    /// </summary>
    private void AddContainerToManager(string name, UserControl container, OpenFileButton.TypeWindow fileType)
    {
      var controlManager = CreateControlManager();
      controlManager.AddControl(name, container, fileType);
      LogDebug($"Контейнер \"{name}\" добавлен в интерфейс ({fileType}).");
    }

    #endregion

    #region ⚙️ Вспомогательное

    /// <summary>
    /// Создаёт экземпляр <see cref="ControlManager"/> для работы с контейнерами.
    /// </summary>
    private ControlManager CreateControlManager()
    {
      return new ControlManager(_fileManager.EditorWorkspaceModel);
    }

    #endregion
  }
}
