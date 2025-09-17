using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MainWindowProgram.Services;

namespace MainWindowProgram.ViewModels
{
  /// <summary>
  /// ViewModel для административного раздела интерфейса.
  /// Предоставляет команды для взаимодействия с административными функциями:
  /// работа с ППУ, логами, USB и отправкой команд.
  /// </summary>
  public partial class AdminViewModel
  {
    private readonly AdminServices _service;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AdminViewModel"/>.
    /// </summary>
    public AdminViewModel(AdminServices service)
    {
      _service = service;
    }

    /// <summary>Команда открытия интерфейса управления ППУ.</summary>
    [RelayCommand]
    private async Task Gpt() => await _service.OpenGptServiceAsync();

    /// <summary>Команда открытия интерфейса отправки произвольной команды.</summary>
    [RelayCommand]
    private async Task SendCommand() => await _service.OpenSendCommand();

    /// <summary>Команда открытия интерфейса просмотра логов.</summary>
    [RelayCommand]
    private async Task Logger() => await _service.OpenLogger();

    /// <summary>Команда открытия интерфейса работы с USB-устройствами.</summary>
    [RelayCommand]
    private async Task Usb() => await _service.OpenUsbServiceAsync();

    [RelayCommand]
    private async Task Protocol() => await _service.ProtocolTest();

    [RelayCommand]
    private async Task Choice() => await _service.ChoiceTest();

    [RelayCommand]
    private async Task Self() => await _service.SelfCheckTest();

    /// <summary>Команда открытия настроек погрешностей выполнения режимов.</summary>
    [RelayCommand]
    private async Task Error() => await _service.OpenErrorSync();

    [RelayCommand]
    private async Task Console() => await _service.StartConsoleTest();
  }
}
