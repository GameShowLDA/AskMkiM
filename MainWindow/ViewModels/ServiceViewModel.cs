using CommunityToolkit.Mvvm.Input;
using MainWindowProgram.Services;
using System.Threading.Tasks;

namespace MainWindowProgram.ViewModels
{
  /// <summary>
  /// ViewModel для управления сервисами.
  /// Содержит команды для отображения элементов управления различных типов сервисов в редакторе.
  /// </summary>
  public partial class ServiceViewModel
  {
    private readonly ServiceMode _serviceMode;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="ServiceViewModel"/>.
    /// </summary>
    public ServiceViewModel(ServiceMode serviceMode)
    {
      _serviceMode = serviceMode;
    }

    /// <summary>Команда отображения сервисного режима модуля типа МеШ.</summary>
    [RelayCommand]
    private async Task ServicesTestMesh() => await _serviceMode.AddServicesTestMeshControlAsync();

    /// <summary>Команда отображения сервисного режима модуля типа МИНТ.</summary>
    [RelayCommand]
    private async Task ServicesTestMint() => await _serviceMode.AddServicesTestMintControlAsync();

    /// <summary>Команда отображения сервисного режима модуля типа МКР.</summary>
    [RelayCommand]
    private async Task ServicesTestMkr() => await _serviceMode.AddServicesTestMkrControlAsync();

    /// <summary>Команда отображения сервисного режима модуля типа УКШ.</summary>
    [RelayCommand]
    private async Task ServicesTestUksh() => await _serviceMode.AddServicesTestUkshControlAsync();
  }
}
