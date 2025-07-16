using System.Windows.Input;
using MainWindowProgram.Infrastructure;
using MainWindowProgram.Services;

namespace MainWindowProgram.ViewModels
{
  /// <summary>
  /// ViewModel для управления режимами метрологии.
  /// Содержит команды для открытия интерфейсов всех доступных режимов.
  /// </summary>
  public class MetrologyViewModel
  {
    /// <summary>
    /// Команда открытия режима КС.
    /// </summary>
    public ICommand KCCommand { get; }

    /// <summary>
    /// Команда открытия режима ИЕ.
    /// </summary>
    public ICommand IECommand { get; }

    /// <summary>
    /// Команда открытия режима СИ.
    /// </summary>
    public ICommand CICommand { get; }

    /// <summary>
    /// Команда открытия режима ПР T.
    /// </summary>
    public ICommand PR_TCommand { get; }

    /// <summary>
    /// Команда открытия режима ПР.
    /// </summary>
    public ICommand PRCommand { get; }

    /// <summary>
    /// Команда открытия режима ПИ в режиме DCW.
    /// </summary>
    public ICommand PIDCWCommand { get; }

    /// <summary>
    /// Команда открытия режима ПИ в режиме ACW.
    /// </summary>
    public ICommand PIACWCommand { get; }

    /// <summary>
    /// Команда открытия режима КН в режиме ACW.
    /// </summary>
    public ICommand KNACWCommand { get; }

    /// <summary>
    /// Команда открытия режима КН в режиме DCW.
    /// </summary>
    public ICommand KNDCWCommand { get; }

    /// <summary>
    /// Сервис для работы с режимами метрологии.
    /// </summary>
    private readonly MetrologyService _service;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="MetrologyViewModel"/>.
    /// </summary>
    /// <param name="service">Сервис для управления метрологическими режимами.</param>
    public MetrologyViewModel(MetrologyService service)
    {
      _service = service;

      KCCommand = new AsyncRelayCommand(_service.OpenKCModeAsync);
      IECommand = new AsyncRelayCommand(_service.OpenIEModeAsync);
      CICommand = new AsyncRelayCommand(_service.OpenCIModeAsync);
      PR_TCommand = new AsyncRelayCommand(_service.OpenPR_TModeAsync);
      PIDCWCommand = new AsyncRelayCommand(_service.OpenPIDCWModeAsync);
      PIACWCommand = new AsyncRelayCommand(_service.OpenPIACWModeAsync);
      KNACWCommand = new AsyncRelayCommand(_service.OpenKNACWModeAsync);
      KNDCWCommand = new AsyncRelayCommand(_service.OpenKNDCWModeAsync);
      PRCommand = new AsyncRelayCommand(_service.OpenPRModeAsync);
    }
  }
}
