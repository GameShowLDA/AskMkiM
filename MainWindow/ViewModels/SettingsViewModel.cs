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
    /// Команда открытия настроек погрешностей выполнения режимов.
    /// </summary>
    public ICommand ErrorCommand { get; }

    public ICommand HelpCommand { get; }

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
      ErrorCommand = new AsyncRelayCommand(_service.OpenErrorSync);
      HelpCommand = new AsyncRelayCommand(_service.HelpTextAsync);
    }
  }
}
