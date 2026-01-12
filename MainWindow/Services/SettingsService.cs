using Ask.Support;
using UI.Controls.Settings;
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
    /// Открывает WebView2 со справочником к программе.
    /// </summary>
    public void OpenSettings() => _multiWindow.AddControl("Параметры", new SettingsProgrammControl(), TypeWindow.Settings);

    /// <summary>
    /// Открывает справочник на нужной странице
    /// </summary>
    /// <param name="value">id страницы справочника</param>
    public void HelpText(string value) => HelpProvider.ShowHelp(value);

    /// <summary>
    /// Открывает раздел "Общая информация" в справочнике
    /// </summary>
    public void HelpOpenGeneralInformation() => HelpText("GeneralInformation");

    /// <summary>
    /// Открывает раздел "Язык программ контроля" в справочнике
    /// </summary>
    public void HelpOpenLanguageControlPrograms() => HelpText("LanguageControlPrograms");

    /// <summary>
    /// Открывает раздел "Состав программы" в справочнике
    /// </summary>
    public void HelpOpenProgramComposition() => HelpText("ProgramComposition");

    /// <summary>
    /// Открывает страницу "О программе" в справочнике
    /// </summary>
    public void HelpOpenAboutProgram() => HelpText("AboutProgram");

    /// <summary>
    /// Открывает станицу "Быстрое меню команд"
    /// </summary>
    public void HelpOpenFastMenuCommand() => HelpProvider.OpenFastMenuCommand();
  }
}
