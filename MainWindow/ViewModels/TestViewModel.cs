using CommunityToolkit.Mvvm.Input;
using MainWindowProgram.Services;
using System.Threading.Tasks;

namespace MainWindowProgram.ViewModels
{
  /// <summary>
  /// ViewModel для управления тестами.
  /// Содержит команды для отображения элементов управления различных типов тестов в редакторе.
  /// </summary>
  public partial class TestViewModel
  {
    private readonly TestService _testService;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="TestViewModel"/>.
    /// </summary>
    /// <param name="testService">Сервис для работы с тестами.</param>
    public TestViewModel(TestService testService)
    {
      _testService = testService;
    }

    /// <summary>Метод узла СИ.</summary>
    [RelayCommand]
    private async Task CiNodeMethod() => await _testService.AddCiNodeMethodControlAsync();

    /// <summary>Метод узла ПИ (DCW).</summary>
    [RelayCommand]
    private async Task PiDCWNodeMethod() => await _testService.AddPiDCWNodeMethodControlAsync();

    /// <summary>Метод узла ПИ (ACW).</summary>
    [RelayCommand]
    private async Task PiACWNodeMethod() => await _testService.AddPiACWNodeMethodControlAsync();

    /// <summary>Групповой метод СИ.</summary>
    [RelayCommand]
    private async Task CiMethodExecutor() => await _testService.AddCiMethodExecutorControlAsync();

    /// <summary>Групповой метод ПИ (ACW).</summary>
    [RelayCommand]
    private async Task PiACWMethodExecutor() => await _testService.AddPiACWMethodExecutorControlAsync();

    /// <summary>Групповой метод ПИ (DCW).</summary>
    [RelayCommand]
    private async Task PiDCWMethodExecutor() => await _testService.AddPiDCWMethodExecutorControlAsync();

    /// <summary>Перекрёстный тест МКР.</summary>
    [RelayCommand]
    private async Task CrossTestMkrExecutor() => await _testService.AddCrossTestMkrExecutorControlAsync();
  }
}
