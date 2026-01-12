using CommunityToolkit.Mvvm.Input;
using MainWindowProgram.Services;

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
    private void KC() => _service.OpenKCModeAsync();

    /// <summary>Открыть режим ИЕ.</summary>
    [RelayCommand]
    private void IE() => _service.OpenIEModeAsync();

    /// <summary>Открыть режим СИ.</summary>
    [RelayCommand]
    private void CI() => _service.OpenCIModeAsync();

    /// <summary>Открыть режим ПР.</summary>
    [RelayCommand]
    private void PR() => _service.OpenPRModeAsync();

    /// <summary>Открыть режим ПИ (DCW).</summary>
    [RelayCommand]
    private void PIDCW() => _service.OpenPIDCWModeAsync();

    /// <summary>Открыть режим ПИ (ACW).</summary>
    [RelayCommand]
    private void PIACW() => _service.OpenPIACWModeAsync();

    /// <summary>Открыть режим КН (ACW).</summary>
    [RelayCommand]
    private void KNACW() => _service.OpenKNACWModeAsync();

    /// <summary>Открыть режим КН (DCW).</summary>
    [RelayCommand]
    private void KNDCW() => _service.OpenKNDCWModeAsync();

    /// <summary>Открыть режим ЭТ.</summary>
    [RelayCommand]
    private void EHT() => _service.OpenEHTModeAsync();
  }
}
