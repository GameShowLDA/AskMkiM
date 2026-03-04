using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Core.Shared.Metadata.Static;
using Ask.Core.Shared.Metadata.View.EditorHost;
using Message;
using System.Windows;
using UI.Components.Invoke;
using UI.Components.MultiEditorMethods;
using UI.Controls.Runner;
using UI.Controls.TextEditor;
using static Ask.LogLib.LoggerUtility;

namespace UI.Services
{
  /// <summary>
  /// Сервис управления элементами управления запуском (<see cref="RunControl"/>) в интерфейсе приложения.
  /// 
  /// Основные задачи:
  /// <list type="bullet">
  ///   <item>Создание и добавление вкладок управления запуском в контейнер редактора.</item>
  ///   <item>Отображение интерфейса управления устройствами или алгоритмами.</item>
  ///   <item>Закрытие и удаление вкладок управления запуском.</item>
  /// </list>
  /// </summary>
  public class RunControlService : IRunService
  {
    private readonly UI.Components.MultiEditorMethods.FileManager _fileManager;

    /// <summary>
    /// Создаёт новый экземпляр сервиса управления вкладками <see cref="RunControl"/>.
    /// </summary>
    /// <param name="fileManager">Главный файловый менеджер, обеспечивающий доступ к контейнерам и модели рабочего пространства.</param>
    public RunControlService(UI.Components.MultiEditorMethods.FileManager fileManager)
    {
      _fileManager = fileManager;
    }

    /// <summary>
    /// Добавляет вкладку управления запуском (<see cref="RunControl"/>) в интерфейс редактора.
    /// 
    /// Если контейнер для заданного типа редактора не существует — он будет создан автоматически.
    /// </summary>
    /// <param name="runControl">Экземпляр элемента управления запуском.</param>
    /// <param name="editorType">Тип контейнера, в который нужно добавить вкладку (например, <see cref="EditorType.Run"/>).</param>
    public async Task AddRunItem(IRunView runControl, EditorType editorType)
    {
      try
      {
        TextEditorContainer runContainer = _fileManager.ContainerService.GetEditorContainer(editorType)
          ?? _fileManager.ContainerService.CreateEditorContainer(editorType, TypeWindow.DeviceControl);

        _fileManager.DockItemService.ShowEditorDockItem(runControl.FileName, runContainer, runControl.View, editorType);
        _fileManager.ControlManagerService.ShowEditorContainer(runContainer, EditorType.Translator);
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show($"Ошибка при добавлении вкладки: {ex.Message}", "Ошибка", image: MessageBoxImage.Error);
        LogException("Ошибка при добавлении вкладки управления запуском", ex);
      }
    }

    /// <summary>
    /// Закрывает вкладку управления запуском (<see cref="RunControl"/>) и удаляет её из контейнера редактора.
    /// </summary>
    /// <param name="runControl">Экземпляр элемента управления запуском, который необходимо закрыть.</param>
    /// <param name="editorType">Тип контейнера, из которого нужно удалить вкладку.</param>
    public async Task CloseRunItem(IRunView runControl, EditorType editorType)
    {
      try
      {
        var controlManager = new ControlManager(_fileManager, _fileManager.EditorWorkspaceModel.MultiEditorControl);
        TextEditorContainer runContainer = _fileManager.ContainerService.GetEditorContainer(editorType);

        var foundTab = _fileManager.EditorWorkspaceModel.OpenPages.FirstOrDefault(tab => tab.Text == editorType.ToString());
        if (foundTab != null)
        {
          await controlManager.RemoveControl(foundTab, runControl.View);
        }
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show($"Ошибка при закрытии вкладки: {ex.Message}", "Ошибка", image: MessageBoxImage.Error);
        LogException("Ошибка при закрытии вкладки управления запуском", ex);
      }
    }
  }
}
