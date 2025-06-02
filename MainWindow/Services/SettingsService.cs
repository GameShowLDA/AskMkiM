using System.Diagnostics;
using System.IO;
using Mode.Settings.DeviceConfig;
using Mode.Settings.Execution;
using Utilities.Help;
using static UI.Components.Invoke.OpenFileButton;

namespace MainWindowProgram.Services
{
  /// <summary>
  /// Реализация сервиса настроек.
  /// Отвечает за отображение соответствующих пользовательских элементов управления в интерфейсе.
  /// </summary>
  public class SettingsService
  {
    /// <summary>
    /// Сервис для управления многооконным пользовательским интерфейсом.
    /// </summary>
    private readonly MultiWindowService _multiWindow;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="SettingsService"/>.
    /// </summary>
    /// <param name="multiWindow">Сервис управления многооконным интерфейсом.</param>
    public SettingsService(MultiWindowService multiWindow)
    {
      _multiWindow = multiWindow;
    }

    /// <summary>
    /// Открывает пользовательский элемент управления с конфигурацией оборудования.
    /// </summary>
    /// <returns>Задача, представляющая операцию открытия интерфейса конфигурации.</returns>
    public async Task OpenConfigurationAsync() =>
      await _multiWindow.AddControlAsync("Конфигурация оборудования", new DeviceConfigControl(), TypeWindow.Settings);

    /// <summary>
    /// Открывает пользовательский элемент управления с настройками выполнения режимов.
    /// </summary>
    /// <returns>Задача, представляющая операцию открытия интерфейса выполнения.</returns>
    public async Task OpenExecutionAsync() =>
      await _multiWindow.AddControlAsync("Выполнение", new ExecutionControl(), TypeWindow.Settings);

    /// <summary>
    /// Открывает пользовательский элемент управления с настройками вывода данных в протокол.
    /// </summary>
    /// <returns>Задача, представляющая операцию открытия интерфейса протокола.</returns>
    public async Task OpenProtocolAsync() =>
      await _multiWindow.AddControlAsync("Протокол", new Mode.Settings.ProtocolManager.ProtocolManagerControl(), TypeWindow.Settings);

    public async Task HelpTextAsync() =>
      HelpProvider.ShowHelp("index");
    
  }
}
