using Ask.Core.Shared.Metadata.Enums.UiEnums;
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
    public void OpenSettings() => _multiWindow.WorkspaceService.AddControl("Параметры", new SettingsProgrammControl(), TypeWindow.Settings);

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
    /// Открывает раздел "Основное меню" в справочнике
    /// </summary>
    public void HelpOpenMainMenu() => HelpText("MainMenu");

    /// <summary>
    /// Открывает раздел "Язык АСК" в справочнике
    /// </summary>
    public void HelpOpenLanguageControlPrograms() => HelpText("LanguageControlPrograms");

    /// <summary>
    /// Открывает раздел "Транслятор" в справочнике
    /// </summary>
    public void HelpOpenTranslator() => HelpText("Translator");

    /// <summary>
    /// Открывает раздел "Текстовый редактор" в справочнике
    /// </summary>
    public void HelpOpenTextEditor() => HelpText("TextEditor");

    /// <summary>
    /// Открывает раздел "Горячие клавиши" в справочнике
    /// </summary>
    public void HelpOpenHotKeys() => HelpText("HotKeys");

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
