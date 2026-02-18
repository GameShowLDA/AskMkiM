using Ask.Core.Shared.Metadata.Enums.UiEnums;
using UI.Controls.ExecutorControls.SelfControl;
using static UI.Components.Invoke.OpenFileButton;

namespace MainWindowProgram.Services
{
  public class SelfTestServices
  {
    /// <summary>
    /// Сервис управления многооконным интерфейсом.
    /// </summary>
    private readonly MultiWindowService _multiWindow;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TestService"/>.
    /// </summary>
    /// <param name="multiWindow">Сервис управления многооконным интерфейсом.</param>
    public SelfTestServices(MultiWindowService multiWindow)
    {
      _multiWindow = multiWindow;
    }

    /// <summary>
    /// Добавляет элемент управления для теста методом узла СИ в multiEditors.
    /// </summary>
    public void AddSelfTestModule() => _multiWindow.WorkspaceService.AddControl("Самоконтроль модуля", new ModuleSelfControl(), TypeWindow.DeviceControl);


    /// <summary>
    /// Добавляет элемент управления для теста методом узла СИ в multiEditors.
    /// </summary>
    public void AddSelfTestSystem() => _multiWindow.WorkspaceService.AddControl("Самоконтроль системы", new SystemSelfControl(), TypeWindow.DeviceControl);
  }
}
