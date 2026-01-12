using UI.Controls.ExecutorControls.MetrologyControls;
using static UI.Components.Invoke.OpenFileButton;

namespace MainWindowProgram.Services
{
  /// <summary>
  /// Реализация сервиса управления режимами метрологии.
  /// Осуществляет отображение пользовательских элементов управления для каждого режима через многооконный сервис.
  /// </summary>
  public class MetrologyService
  {
    /// <summary>
    /// Сервис для управления многооконным пользовательским интерфейсом.
    /// </summary>
    private readonly MultiWindowService _multiWindow;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MetrologyService"/>.
    /// </summary>
    /// <param name="multiWindow">Сервис управления многооконным интерфейсом.</param>
    public MetrologyService(MultiWindowService multiWindow)
    {
      _multiWindow = multiWindow;
    }

    /// <summary>
    /// Открывает пользовательский элемент управления режима КС.
    /// </summary>
    public void OpenKCModeAsync() => _multiWindow.AddControl("Режим КС", new KcMetrologyControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Открывает пользовательский элемент управления режима ИЕ.
    /// </summary>
    public void OpenIEModeAsync() => _multiWindow.AddControl("Режим ИЕ", new IeMetrologyControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Открывает пользовательский элемент управления режима СИ.
    /// </summary>
    public void OpenCIModeAsync() => _multiWindow.AddControl("Режим СИ", new CiMetrologyControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Открывает пользовательский элемент управления режима ПР.
    /// </summary>
    public void OpenPRModeAsync() => _multiWindow.AddControl("Режим ПР", new PrMetrologyControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Открывает пользовательский элемент управления режима ПИ (DCW).
    /// </summary>
    public void OpenPIDCWModeAsync() => _multiWindow.AddControl("Режим ПИ(DCW)", new PiDCWMetrologyControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Открывает пользовательский элемент управления режима ПИ (ACW).
    /// </summary>
    public void OpenPIACWModeAsync() => _multiWindow.AddControl("Режим ПИ(ACW)", new PiACWMetrologyControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Открывает пользовательский элемент управления режима КН (ACW).
    /// </summary>
    public void OpenKNACWModeAsync() => _multiWindow.AddControl("Режим КН(ACW)", new KnACWMetrologyControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Открывает пользовательский элемент управления режима КН (DCW).
    /// </summary>
    public void OpenKNDCWModeAsync() => _multiWindow.AddControl("Режим КН(DCW)", new KnDCWMetrologyControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Открывает пользовательский элемент управления режима КН (DCW).
    /// </summary>
    public void OpenEHTModeAsync() => _multiWindow.AddControl("Режим ЭТ", new EhtMetrologyControl(), TypeWindow.DeviceControl);
  }
}
