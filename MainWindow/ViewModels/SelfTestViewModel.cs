using CommunityToolkit.Mvvm.Input;
using MainWindowProgram.Services;
using System.Threading.Tasks;

namespace MainWindowProgram.ViewModels
{
  /// <summary>
  /// ViewModel для самотестирования системы и модулей.
  /// </summary>
  public partial class SelfTestViewModel
  {
    private readonly SelfTestServices _selfTestServices;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="SelfTestViewModel"/>.
    /// </summary>
    /// <param name="testService">Сервис для работы с самотестированием.</param>
    public SelfTestViewModel(SelfTestServices testService)
    {
      _selfTestServices = testService;
    }

    /// <summary>Команда самотеста модуля СИ.</summary>
    [RelayCommand]
    private async Task SelfTestModule() => await _selfTestServices.AddSelfTestModuleAsync();

    /// <summary>Команда самотеста всей системы.</summary>
    [RelayCommand]
    private async Task SelfTestSystem() => await _selfTestServices.AddSelfTestSystemAsync();
  }
}
