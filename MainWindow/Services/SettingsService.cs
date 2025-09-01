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

    /// <summary>
    /// Открывает WebView2 со справочником к программе.
    /// </summary>
    /// <returns>Задача, представляющая операцию открытия интерфейса протокола.</returns>
    public async Task OpenSettingsAsync() =>
      await _multiWindow.AddControlAsync("Параметры", new Mode.Settings.SettingsProgramm.SettingsProgrammControl(), TypeWindow.Settings);

    /// <summary>
    /// Открывает справочник на нужной странице
    /// </summary>
    /// <param name="value">id страницы справочника</param>
    public static void HelpTextAsync(string value) =>
      HelpProvider.ShowHelp(value);

    /// <summary>
    /// Открывает раздел "Общая информация" в справочнике
    /// </summary>
    /// <returns>Задача, представляющая операцию открытия локального сервера с открытым справочником</returns>
    public async Task HelpOpenGeneralInformation() => HelpTextAsync("GeneralInformation");

    /// <summary>
    /// Открывает раздел "Язык программ контроля" в справочнике
    /// </summary>
    /// <returns>Задача, представляющая операцию открытия локального сервера с открытым справочником</returns>
    public async Task HelpOpenLanguageControlPrograms() => HelpTextAsync("LanguageControlPrograms");

    /// <summary>
    /// Открывает раздел "Состав программы" в справочнике
    /// </summary>
    /// <returns>Задача, представляющая операцию открытия локального сервера с открытым справочником</returns>
    public async Task HelpOpenProgramComposition() => HelpTextAsync("ProgramComposition");

    /// <summary>
    /// Открывает страницу "О программе" в справочнике
    /// </summary>
    /// <returns>Задача, представляющая операцию открытия локального сервера с открытым справочником</returns>
    public async Task HelpOpenAboutProgram() => HelpTextAsync("AboutProgram");

    /// <summary>
    /// Открывает станицу "Быстрое меню команд"
    /// </summary>
    /// <returns>Задача, представляющая операцию открытия локального сервера с открытым меню</returns>
    public async Task HelpOpenFastMenuCommand() => HelpProvider.OpenFastMenuCommand();
  }
}
