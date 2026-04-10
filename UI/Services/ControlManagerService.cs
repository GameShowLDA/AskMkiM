using Ask.Core.Shared.Metadata.Static;
using UI.Components.Invoke;
using UI.Components.MultiEditorMethods;
using UI.Controls.TextEditorControl;
using static Ask.LogLib.LoggerUtility;

namespace UI.Services
{
  /// <summary>
  /// Сервис управления отображением контейнеров редактора в пользовательском интерфейсе.
  /// 
  /// Основные задачи:
  /// <list type="bullet">
  ///   <item>Отображение контейнера вкладок указанного типа.</item>
  ///   <item>Создание кнопки вкладки и её привязка к контейнеру.</item>
  ///   <item>Регистрация контейнера в системе управления вкладками.</item>
  /// </list>
  /// </summary>
  public class ControlManagerService
  {
    private readonly EditorWorkspaceModel _context;

    /// <summary>
    /// Создаёт новый экземпляр сервиса управления отображением контейнеров.
    /// </summary>
    public ControlManagerService(EditorWorkspaceModel editorWorkspaceModel)
    {
      _context = editorWorkspaceModel;
    }

    /// <summary>
    /// Отображает контейнер с вкладками заданного типа в интерфейсе.
    /// </summary>
    /// <param name="container">Контейнер, содержащий вкладки указанного типа.</param>
    /// <param name="editorType">Тип вкладок (например, редактор текста, архив, протокол и т.д.).</param>
    public void ShowEditorContainer(TextEditorContainer container, EditorType editorType)
    {
      LogDebug($"Отображение контейнера для типа \"{editorType}\"");

      var controlManager = CreateControlManager();
      var tabButton = CreateTabButton(editorType);

      controlManager.ShowControl(container, tabButton);
    }

    #region 🔧 Вспомогательные методы

    /// <summary>
    /// Создаёт новый экземпляр <see cref="ControlManager"/> для работы с контейнерами.
    /// </summary>
    private ControlManager CreateControlManager()
    {
      return new ControlManager(_context);
    }

    /// <summary>
    /// Формирует кнопку вкладки для указанного типа редактора.
    /// </summary>
    private OpenFileButton CreateTabButton(EditorType editorType)
    {
      return new OpenFileButton
      {
        Header = { Text = editorType.ToString() }
      };
    }

    #endregion
  }
}
