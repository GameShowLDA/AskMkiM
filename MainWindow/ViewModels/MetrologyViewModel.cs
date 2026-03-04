using Ask.Core.Shared.Metadata.Enums.MetrologyEnums;
using Ask.Core.Shared.Metadata.View;
using CommunityToolkit.Mvvm.Input;

namespace MainWindowProgram.ViewModels
{
  /// <summary>
  /// ViewModel для управления режимами метрологии.
  /// Содержит команды для открытия интерфейсов всех доступных режимов.
  /// </summary>
  public partial class MetrologyViewModel
  {
    private readonly IMetrologyServiceView _service;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MetrologyViewModel"/>.
    /// </summary>
    public MetrologyViewModel(IMetrologyServiceView service)
    {
      _service = service;
    }

    /// <summary>Открыть режим КС.</summary>
    [RelayCommand]
    private void KC() => _service.OpenMetrologyMode(MetrologyType.KC);

    /// <summary>Открыть режим ИЕ.</summary>
    [RelayCommand]
    private void IE() => _service.OpenMetrologyMode(MetrologyType.IE);

    /// <summary>Открыть режим СИ.</summary>
    [RelayCommand]
    private void CI() => _service.OpenMetrologyMode(MetrologyType.SI);

    /// <summary>Открыть режим ПР.</summary>
    [RelayCommand]
    private void PR() => _service.OpenMetrologyMode(MetrologyType.PR);

    /// <summary>Открыть режим ПИ (DCW).</summary>
    [RelayCommand]
    private void PIDCW() => _service.OpenMetrologyMode(MetrologyType.PI_DCW);

    /// <summary>Открыть режим ПИ (ACW).</summary>
    [RelayCommand]
    private void PIACW() => _service.OpenMetrologyMode(MetrologyType.PI_ACW);

    /// <summary>Открыть режим КН (ACW).</summary>
    [RelayCommand]
    private void KNACW() => _service.OpenMetrologyMode(MetrologyType.KN_ACW);

    /// <summary>Открыть режим КН (DCW).</summary>
    [RelayCommand]
    private void KNDCW() => _service.OpenMetrologyMode(MetrologyType.KN_DCW);

    /// <summary>Открыть режим ЭТ.</summary>
    [RelayCommand]
    private void EHT() => _service.OpenMetrologyMode(MetrologyType.EHT);
  }
}
