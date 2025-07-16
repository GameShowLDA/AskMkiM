using Mode.Metrology.CI;
using Mode.Metrology.IE;
using Mode.Metrology.KC;
using Mode.Metrology.KN;
using Mode.Metrology.PI;
using Mode.Metrology.PR;
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
    public async Task OpenKCModeAsync() =>
        await _multiWindow.AddControlAsync("Режим КС", new KcMetrologyControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Открывает пользовательский элемент управления режима ИЕ.
    /// </summary>
    public async Task OpenIEModeAsync() =>
        await _multiWindow.AddControlAsync("Режим ИЕ", new IeMetrologyControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Открывает пользовательский элемент управления режима СИ.
    /// </summary>
    public async Task OpenCIModeAsync() =>
        await _multiWindow.AddControlAsync("Режим СИ", new CiMetrologyControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Открывает пользовательский элемент управления режима ПР.
    /// </summary>
    public async Task OpenPR_TModeAsync() =>
        await _multiWindow.AddControlAsync("Режим ПР Т", new PrTMetrologyControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Открывает пользовательский элемент управления режима ПР.
    /// </summary>
    public async Task OpenPRModeAsync() =>
        await _multiWindow.AddControlAsync("Режим ПР", new PrMetrologyControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Открывает пользовательский элемент управления режима ПИ (DCW).
    /// </summary>
    public async Task OpenPIDCWModeAsync() =>
        await _multiWindow.AddControlAsync("Режим ПИ(DCW)", new PiDCWMetrologyControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Открывает пользовательский элемент управления режима ПИ (ACW).
    /// </summary>
    public async Task OpenPIACWModeAsync() =>
        await _multiWindow.AddControlAsync("Режим ПИ(ACW)", new PiACWMetrologyControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Открывает пользовательский элемент управления режима КН (ACW).
    /// </summary>
    public async Task OpenKNACWModeAsync() =>
        await _multiWindow.AddControlAsync("Режим КН(ACW)", new KnACWMetrologyControl(), TypeWindow.DeviceControl);

    /// <summary>
    /// Открывает пользовательский элемент управления режима КН (DCW).
    /// </summary>
    public async Task OpenKNDCWModeAsync() =>
        await _multiWindow.AddControlAsync("Режим КН(DCW)", new KnDCWMetrologyControl(), TypeWindow.DeviceControl);
  }
}
