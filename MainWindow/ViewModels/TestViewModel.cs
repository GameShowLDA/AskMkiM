using System.Windows.Input;
using MainWindowProgram.Infrastructure;
using MainWindowProgram.Services;

namespace MainWindowProgram.ViewModels
{
  /// <summary>
  /// ViewModel для управления тестами.
  /// Содержит команды для отображения элементов управления различных типов тестов в редакторе.
  /// </summary>
  public class TestViewModel
  {
    /// <summary>
    /// Сервис для работы с тестами.
    /// </summary>
    private readonly TestService _testService;

    /// <summary>
    /// Команда отображения метода узла СИ.
    /// </summary>
    public ICommand CiNodeMethodCommand { get; }

    /// <summary>
    /// Команда отображения метода узла ПИ (DCW).
    /// </summary>
    public ICommand PiDCWNodeMethodCommand { get; }

    /// <summary>
    /// Команда отображения метода узла ПИ (ACW).
    /// </summary>
    public ICommand PiACWNodeMethodCommand { get; }

    /// <summary>
    /// Команда отображения группового метода СИ.
    /// </summary>
    public ICommand CiMethodExecutorCommand { get; }

    /// <summary>
    /// Команда отображения группового метода ПИ (ACW).
    /// </summary>
    public ICommand PiACWMethodExecutorCommand { get; }

    /// <summary>
    /// Команда отображения группового метода ПИ (DCW).
    /// </summary>
    public ICommand PiDCWMethodExecutorCommand { get; }

    /// <summary>
    /// Команда отображения перекрёстного теста МКР.
    /// </summary>
    public ICommand CrossTestMkrExecutorCommand { get; }

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TestViewModel"/>.
    /// </summary>
    /// <param name="testService">Сервис для работы с тестами.</param>
    public TestViewModel(TestService testService)
    {
      _testService = testService;

      CiNodeMethodCommand = new AsyncRelayCommand(_testService.AddCiNodeMethodControlAsync);
      PiDCWNodeMethodCommand = new AsyncRelayCommand(_testService.AddPiDCWNodeMethodControlAsync);
      PiACWNodeMethodCommand = new AsyncRelayCommand(_testService.AddPiACWNodeMethodControlAsync);

      CiMethodExecutorCommand = new AsyncRelayCommand(_testService.AddCiMethodExecutorControlAsync);
      PiACWMethodExecutorCommand = new AsyncRelayCommand(_testService.AddPiACWMethodExecutorControlAsync);
      PiDCWMethodExecutorCommand = new AsyncRelayCommand(_testService.AddPiDCWMethodExecutorControlAsync);

      CrossTestMkrExecutorCommand = new AsyncRelayCommand(_testService.AddCrossTestMkrExecutorControlAsync);
    }
  }
}
