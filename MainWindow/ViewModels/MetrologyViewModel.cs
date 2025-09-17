using CommunityToolkit.Mvvm.Input;
using MainWindowProgram.Services;
using System.Threading.Tasks;

namespace MainWindowProgram.ViewModels
{
  /// <summary>
  /// ViewModel для управления режимами метрологии.
  /// Содержит команды для открытия интерфейсов всех доступных режимов.
  /// </summary>
  public partial class MetrologyViewModel
  {
    private readonly MetrologyService _service;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MetrologyViewModel"/>.
    /// </summary>
    public MetrologyViewModel(MetrologyService service)
    {
      _service = service;
    }

    /// <summary>Открыть режим КС.</summary>
    [RelayCommand]
    private async Task KC() => await _service.OpenKCModeAsync();

    /// <summary>Открыть режим ИЕ.</summary>
    [RelayCommand]
    private async Task IE() => await _service.OpenIEModeAsync();

    /// <summary>Открыть режим СИ.</summary>
    [RelayCommand]
    private async Task CI() => await _service.OpenCIModeAsync();

    /// <summary>Открыть режим ПР T.</summary>
    [RelayCommand]
    private async Task PR_T() => await _service.OpenPR_TModeAsync();

    /// <summary>Открыть режим ПР.</summary>
    [RelayCommand]
    private async Task PR() => await _service.OpenPRModeAsync();

    /// <summary>Открыть режим ПИ (DCW).</summary>
    [RelayCommand]
    private async Task PIDCW() => await _service.OpenPIDCWModeAsync();

    /// <summary>Открыть режим ПИ (ACW).</summary>
    [RelayCommand]
    private async Task PIACW() => await _service.OpenPIACWModeAsync();

    /// <summary>Открыть режим КН (ACW).</summary>
    [RelayCommand]
    private async Task KNACW() => await _service.OpenKNACWModeAsync();

    /// <summary>Открыть режим КН (DCW).</summary>
    [RelayCommand]
    private async Task KNDCW() => await _service.OpenKNDCWModeAsync();
  }
}
