using CommunityToolkit.Mvvm.Input;
using MainWindowProgram.Services;

namespace MainWindowProgram.ViewModels
{
  /// <summary>
  /// ViewModel для управления настройками приложения.
  /// Содержит команды для открытия различных вкладок с параметрами конфигурации, выполнения и протоколирования.
  /// </summary>
  public partial class SettingsViewModel
  {
    private readonly SettingsService _service;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="SettingsViewModel"/>.
    /// </summary>
    public SettingsViewModel(SettingsService service)
    {
      _service = service;
    }

    /// <summary>Открыть раздел "Общие сведения" в справочнике.</summary>
    [RelayCommand]
    private void HelpOpenGeneralInformation() => _service.HelpOpenGeneralInformation();

    /// <summary>Открыть раздел "Основное меню" в справочнике.</summary>
    [RelayCommand]
    private void HelpOpenMainMenu() => _service.HelpOpenMainMenu();

    /// <summary>Открыть раздел "Язык АСК" в справочнике.</summary>
    [RelayCommand]
    private void HelpOpenLanguageControlPrograms() => _service.HelpOpenLanguageControlPrograms();

    /// <summary>Открыть раздел "Транслятор" в справочнике.</summary>
    [RelayCommand]
    private void HelpOpenTranslator() => _service.HelpOpenTranslator();

    /// <summary>Открыть раздел "Текстовый редактор" в справочнике.</summary>
    [RelayCommand]
    private void HelpOpenTextEditor() => _service.HelpOpenTextEditor();

    /// <summary>Открыть раздел "Горячие клавиши" в справочнике.</summary>
    [RelayCommand]
    private void HelpOpenHotKeys() => _service.HelpOpenHotKeys();

    /// <summary>Открыть раздел "О программе" в справочнике.</summary>
    [RelayCommand]
    private void HelpOpenAboutProgram() => _service.HelpOpenAboutProgram();

    /// <summary>Открыть быстрое меню команд.</summary>
    [RelayCommand]
    private void HelpOpenFastMenu() => _service.HelpOpenFastMenuCommand();

    /// <summary>Открыть общие настройки приложения.</summary>
    [RelayCommand]
    private void Settings() => _service.OpenSettings();
  }
}
