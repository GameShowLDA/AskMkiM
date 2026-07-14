using CommunityToolkit.Mvvm.Input;
using MainWindowProgram.Services;

namespace MainWindowProgram.ViewModels
{
  /// <summary>
  /// ViewModel для управления тестами.
  /// Содержит команды для отображения элементов управления разных групп тестов.
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

    /// <summary>Открывает метод узла СИ.</summary>
    [RelayCommand]
    private void CiNodeMethod() => _testService.AddCiNodeMethodControlAsync();

    /// <summary>Открывает метод узла ПИ (DCW).</summary>
    [RelayCommand]
    private void PiDCWNodeMethod() => _testService.AddPiDCWNodeMethodControlAsync();

    /// <summary>Открывает метод узла ПИ (ACW).</summary>
    [RelayCommand]
    private void PiACWNodeMethod() => _testService.AddPiACWNodeMethodControlAsync();

    /// <summary>Открывает групповой метод СИ.</summary>
    [RelayCommand]
    private void CiMethodExecutor() => _testService.AddCiMethodExecutorControlAsync();

    /// <summary>Открывает групповой метод ПИ (ACW).</summary>
    [RelayCommand]
    private void PiACWMethodExecutor() => _testService.AddPiACWMethodExecutorControlAsync();

    /// <summary>Открывает групповой метод ПИ (DCW).</summary>
    [RelayCommand]
    private void PiDCWMethodExecutor() => _testService.AddPiDCWMethodExecutorControlAsync();

    /// <summary>Открывает перекрестный тест МКР.</summary>
    [RelayCommand]
    private void CrossTestMkrExecutor() => _testService.AddCrossTestMkrExecutorControlAsync();

    /// <summary>Открывает контроль сопротивления контактов реле коммутатора.</summary>
    [RelayCommand]
    private void RelayContactResistExecutor() => _testService.AddRelayContactResistExecutorControlAsync();

  }
}
