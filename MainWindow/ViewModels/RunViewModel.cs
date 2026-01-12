using MainWindowProgram.Services;

namespace MainWindowProgram.ViewModels
{
  public class RunViewModel
  {
    /// <summary>
    /// Сервис административных функций.
    /// </summary>
    private readonly RunServices _service;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="RunViewModel"/>.
    /// </summary>
    /// <param name="service">Сервис административных функций.</param>
    public RunViewModel(RunServices service)
    {
      _service = service;
      //RunCommand = new AsyncRelayCommand(_service.RunAsync);
    }
  }
}
