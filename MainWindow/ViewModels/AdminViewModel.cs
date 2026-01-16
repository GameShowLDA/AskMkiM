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
    private void Gpt() => _service.OpenGptServiceAsync();

    /// <summary>Команда открытия интерфейса работы с USB-устройствами.</summary>
    [RelayCommand]
    private async Task Usb() => await _service.OpenUsbServiceAsync();

    [RelayCommand]
    private async Task AdminPanel() => _service.AdminPanel();

    [RelayCommand]
    private void Protocol() => _service.ProtocolTest();

    [RelayCommand]
    private void ProtocolBase() => _service.ProtocolBaseTest();

    [RelayCommand]
    private async Task Console() => await _service.StartConsoleTest();
  }
}
