using System.Windows.Input;
using MainWindowProgram.Infrastructure;
using MainWindowProgram.Services;

namespace MainWindowProgram.ViewModels
{
  /// <summary>
  /// ViewModel для управления настройками приложения.
  /// Содержит команды для открытия различных вкладок с параметрами конфигурации, выполнения и протоколирования.
  /// </summary>
  public class SettingsViewModel
  {
    /// <summary>
    /// Команда открытия настроек выполнения режимов.
    /// </summary>
    public ICommand ExecutionCommand { get; }

    /// <summary>
    /// Команда открытия настроек конфигурации оборудования.
    /// </summary>
    public ICommand ConfigCommand { get; }

    /// <summary>
    /// Команда открытия настроек протоколирования.
    /// </summary>
    public ICommand ProtocolCommand { get; }

    /// <summary>
    /// Команда открытия справочника.
    /// </summary>
    //public ICommand HelpCommand { get; }

    /// <summary>
    /// Команда открытия раздела "Общие сведенья" в справочнике
    /// </summary>
    public ICommand HelpOpenGeneralInformation {  get; }

    /// <summary>
    /// Команда открытия раздела "Язык программ контроля" в справочнике
    /// </summary>
    public ICommand HelpOpenLanguageControlPrograms { get; }

    /// <summary>
    /// Команда открытия раздела "Состав программы" в справочнике
    /// </summary>
    public ICommand HelpOpenProgramComposition { get; }

    /// <summary>
    /// Команда открытия страницы "О программе" в справочнике
    /// </summary>
    public ICommand HelpOpenAboutProgram { get; }

    /// <summary>
    /// Команда открытия быстрого меню команд
    /// </summary>
    public ICommand HelpOpenFastMenuCommand { get; }

    public ICommand SettingsCommand { get; }

    /// <summary>
    /// Сервис для работы с настройками.
    /// </summary>
    private readonly SettingsService _service;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="SettingsViewModel"/>.
    /// </summary>
    /// <param name="service">Сервис настроек.</param>
    public SettingsViewModel(SettingsService service)
    {
      _service = service;

      ExecutionCommand = new AsyncRelayCommand(_service.OpenExecutionAsync);
      ConfigCommand = new AsyncRelayCommand(_service.OpenConfigurationAsync);
      ProtocolCommand = new AsyncRelayCommand(_service.OpenProtocolAsync);
      //HelpCommand = new AsyncRelayCommand(_service.HelpTextAsync);
      HelpOpenGeneralInformation = new AsyncRelayCommand(_service.HelpOpenGeneralInformation);
      HelpOpenLanguageControlPrograms = new AsyncRelayCommand(_service.HelpOpenLanguageControlPrograms);
      HelpOpenProgramComposition = new AsyncRelayCommand(_service.HelpOpenProgramComposition);
      HelpOpenAboutProgram = new AsyncRelayCommand(_service.HelpOpenAboutProgram);
      SettingsCommand = new AsyncRelayCommand(_service.OpenSettingsAsync);
      HelpOpenFastMenuCommand = new AsyncRelayCommand(_service.HelpOpenFastMenuCommand);
    }
  }
}
