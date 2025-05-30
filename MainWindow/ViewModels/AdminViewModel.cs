using System.Windows.Input;
using MainWindowProgram.Infrastructure;
using MainWindowProgram.Services;

namespace MainWindowProgram.ViewModels
{
  /// <summary>
  /// ViewModel для административного раздела интерфейса.
  /// Предоставляет команды для взаимодействия с административными функциями:
  /// работа с ППУ, логами, USB и отправкой команд.
  /// </summary>
  public class AdminViewModel
  {
    /// <summary>
    /// Команда открытия интерфейса управления ППУ (пробойной установкой).
    /// </summary>
    public ICommand GptCommand { get; }

    /// <summary>
    /// Команда открытия интерфейса отправки произвольной команды.
    /// </summary>
    public ICommand SendCommandCommand { get; }

    /// <summary>
    /// Команда открытия интерфейса просмотра логов.
    /// </summary>
    public ICommand LoggerCommand { get; }

    /// <summary>
    /// Команда открытия интерфейса работы с USB-устройствами.
    /// </summary>
    public ICommand UsbCommand { get; }

    public ICommand ProtocolCommand { get; }
    public ICommand ChoiceCommand { get; }
    public ICommand SelfCommand { get; }

    /// <summary>
    /// Команда открытия настроек погрешностей выполнения режимов.
    /// </summary>
    public ICommand ErrorCommand { get; }

    /// <summary>
    /// Сервис административных функций.
    /// </summary>
    private readonly AdminServices _service;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AdminViewModel"/>.
    /// </summary>
    /// <param name="service">Сервис административных функций.</param>
    public AdminViewModel(AdminServices service)
    {
      _service = service;
      GptCommand = new AsyncRelayCommand(_service.OpenGptServiceAsync);
      SendCommandCommand = new AsyncRelayCommand(_service.OpenSendCommand);
      LoggerCommand = new AsyncRelayCommand(_service.OpenLogger);
      UsbCommand = new AsyncRelayCommand(_service.OpenUsbServiceAsync);
      ProtocolCommand = new AsyncRelayCommand(_service.ProtocolTest);
      ChoiceCommand = new AsyncRelayCommand(_service.ChoiceTest);
      SelfCommand = new AsyncRelayCommand(_service.SelfCheckTest);
      ErrorCommand = new AsyncRelayCommand(_service.OpenErrorSync);
    }
  }
}
