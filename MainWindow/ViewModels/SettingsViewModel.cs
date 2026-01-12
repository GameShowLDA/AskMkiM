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

    /// <summary>Открыть раздел "Язык программ контроля" в справочнике.</summary>
    [RelayCommand]
    private void HelpOpenLanguageControlPrograms() => _service.HelpOpenLanguageControlPrograms();

    /// <summary>Открыть раздел "Состав программы" в справочнике.</summary>
    [RelayCommand]
    private void HelpOpenProgramComposition() => _service.HelpOpenProgramComposition();

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
