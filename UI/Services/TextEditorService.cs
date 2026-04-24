using Ask.Core.Shared.DTO.TextEditor;
using Ask.Core.Shared.Metadata.Enums.FileEnums;
using Ask.Core.Shared.Metadata.Static;
using UI.Components.MultiEditorMethods;
using UI.Controls;
using UI.Controls.TextEditorControl;

namespace UI.Services
{
  /// <summary>
  /// Сервис управления текстовыми редакторами в пользовательском интерфейсе.
  /// 
  /// Основные задачи:
  /// <list type="bullet">
  ///   <item>Создание и инициализация экземпляров <see cref="TextEditorUI"/>.</item>
  ///   <item>Получение активного текстового редактора.</item>
  ///   <item>Закрытие активной вкладки и освобождение ресурсов.</item>
  /// </list>
  /// </summary>
  public class TextEditorService
  {
    private readonly UI.Components.MultiEditorMethods.FileManager _fileManager;

    /// <summary>
    /// Инициализирует новый экземпляр сервиса управления текстовыми редакторами.
    /// </summary>
    /// <param name="fileManager">Главный файловый менеджер, предоставляющий доступ к модели рабочего пространства.</param>
    public TextEditorService(UI.Components.MultiEditorMethods.FileManager fileManager)
    {
      _fileManager = fileManager;
    }

    /// <summary>
    /// Закрывает вкладку с активным текстовым редактором.
    /// </summary>
    /// <param name="isTranslation">Флаг, указывающий, выполняется ли закрытие в процессе трансляции.</param>
    /// <returns><c>true</c>, если вкладка была успешно закрыта; <c>false</c>, если активный редактор не найден.</returns>
    public bool CloseActiveTextEditor(bool isTranslation)
    {
      var container = _fileManager.ContainerService.GetEditorContainer(EditorType.TextEditor);
      if (container == null) return false;

      var activeDockItem = container.DockManager.DockItems.FirstOrDefault(item => item.IsActiveItem);
      if (activeDockItem?.Content is not TextEditorUI) return false;

      return activeDockItem.PerformClose();
    }

    /// <summary>
    /// Создаёт и инициализирует новый экземпляр текстового редактора.
    /// </summary>
    /// <param name="textEditorModel">Модель редактора, содержащая путь и имя файла.</param>
    /// <param name="fileContent">Содержимое, которое будет отображено в редакторе.</param>
    /// <param name="fileType">Тип файла (по умолчанию <see cref="FileType.None"/>).</param>
    /// <returns>Экземпляр <see cref="TextEditorUI"/> с установленным содержимым.</returns>
    public TextEditorUI CreateTextEditor(TextEditorModel textEditorModel, string fileContent, FileType fileType = FileType.None)
    {
      var editor = new TextEditorUI(fileType, textEditorModel)
      {
        Text = fileContent
      };

      if (textEditorModel != null)
      {
        textEditorModel.SavedTextSnapshot = fileContent;
      }

      return editor;
    }

    /// <summary>
    /// Получает активный текстовый редактор для указанного типа редактора.
    /// </summary>
    /// <param name="editorType">Тип редактора (<see cref="EditorType.TextEditor"/> и т.д.).</param>
    /// <returns>Активный <see cref="TextEditorUI"/> или <c>null</c>, если активный редактор не найден.</returns>
    public TextEditorUI GetActiveTextEditor(EditorType editorType)
    {
      var activeTab = _fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(page => page.IsActive);

      if (activeTab == null) return null;

      if (_fileManager.EditorWorkspaceModel.UserControls[_fileManager.EditorWorkspaceModel.OpenPages.IndexOf(activeTab)] is not TextEditorContainer)
        return null;

      var container = _fileManager.ContainerService.GetEditorContainer(editorType);
      if (container == null) return null;

      return editorType == EditorType.TextEditor && activeTab.Text == editorType.ToString()
        ? container.GetTextEditor()
        : null;
    }

    /// <summary>
    /// Получает активный текстовый редактор в текущем открытом контейнере.
    /// </summary>
    /// <returns>Активный экземпляр <see cref="TextEditorUI"/> или <c>null</c>, если активный редактор не найден.</returns>
    public TextEditorUI GetActiveTextEditor()
    {
      var activeTab = _fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(page => page.IsActive);

      if (activeTab == null) return null;

      int index = _fileManager.EditorWorkspaceModel.OpenPages.IndexOf(activeTab);
      if (_fileManager.EditorWorkspaceModel.UserControls[index] is not TextEditorContainer container) return null;

      var activeDockItem = container.DockManager.DockItems.FirstOrDefault(item => item.IsActiveDocument);
      if (activeDockItem == null) return null;

      return activeDockItem.Content switch
      {
        TranslatorItem translatorItem => translatorItem.GetLeftBox().GetTextEditor(),
        TextEditorUI textEditor => textEditor,
        _ => null
      };
    }

    /// <summary>
    /// Возвращает список файлов, открытых во вкладках основного текстового редактора.
    /// </summary>
    public IReadOnlyList<OpenTextEditorDescriptor> GetOpenTextEditors()
    {
      var container = _fileManager.ContainerService.GetEditorContainer(EditorType.TextEditor);
      if (container == null)
      {
        return Array.Empty<OpenTextEditorDescriptor>();
      }

      var result = new List<OpenTextEditorDescriptor>();
      foreach (var dockItem in container.DockManager.DockItems)
      {
        if (dockItem.Content is not TextEditorUI textEditor)
        {
          continue;
        }

        var displayName = textEditor.TextEditorModel?.FileName;
        if (string.IsNullOrWhiteSpace(displayName))
        {
          displayName = dockItem.TabText;
        }

        var filePath = textEditor.TextEditorModel?.FilePath ?? string.Empty;
        result.Add(new OpenTextEditorDescriptor(displayName ?? string.Empty, filePath));
      }

      return result
        .OrderBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }
  }
}
