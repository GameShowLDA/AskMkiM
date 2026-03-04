using Ask.Core.Services.App;
using Ask.Core.Services.Metrology;
using Ask.Core.Shared.Metadata.Enums.MetrologyEnums;
using Ask.Core.Shared.Metadata.Enums.UiEnums;
using Ask.Core.Shared.Metadata.View;
using NLog.Config;
using UI.Controls.ExecutorControls.MetrologyControls;
using static UI.Components.Invoke.OpenFileButton;

namespace MainWindowProgram.Services
{
  /// <summary>
  /// Реализация сервиса управления режимами метрологии.
  /// Осуществляет отображение пользовательских элементов управления для каждого режима через многооконный сервис.
  /// </summary>
  public class MetrologyService : IMetrologyServiceView
  {
    /// <summary>
    /// Сервис для управления многооконным пользовательским интерфейсом.
    /// </summary>
    private readonly MultiWindowService _multiWindow;
    private readonly MetrologyControlFactory _factory;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MetrologyService"/>.
    /// </summary>
    /// <param name="multiWindow">Сервис управления многооконным интерфейсом.</param>
    public MetrologyService(MultiWindowService multiWindow, MetrologyControlFactory factory)
    {
      _multiWindow = multiWindow;
      _factory = factory;
    }

    public void OpenMetrologyMode(MetrologyType type)
    {
      var (control, title) = _factory.Create(type);
      _multiWindow.WorkspaceService.AddControl(title, control, TypeWindow.DeviceControl);
    }
  }
}
