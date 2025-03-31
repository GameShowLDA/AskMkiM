using System.Windows.Controls;
using NewCore.Base.Interface.Additionally;

namespace Mode.Settings.DeviceConfig.DeviceManager
{
  /// <summary>
  /// Логика взаимодействия для DeviceManagerControl.xaml.
  /// </summary>
  public partial class DeviceManagerControl : UserControl
  {
    /// <summary>
    /// Событие, вызываемое при добавлении пробойной установки.
    /// </summary>
    public event EventHandler<IHeadUnit> AddBreakdownEvent;

    /// <summary>
    /// Событие, вызываемое при выборе устройства коммутации шин.
    /// </summary>
    public event EventHandler<IHeadUnit> DeviceBusCommutationSelected;

    /// <summary>
    /// Событие, вызываемое при добавлении быстрого измерителя.
    /// </summary>
    public event EventHandler<IHeadUnit> FastMeterEvent;

    /// <summary>
    /// Событие, вызываемое при добавлении модуля источника питания.
    /// </summary>
    public event EventHandler<IHeadUnit> PowerModuleEvent;

    /// <summary>
    /// Событие, вызываемое при добавлении модуля коммутации реле.
    /// </summary>
    public event EventHandler<IHeadUnit> ModuleRelayEvent;

    /// <summary>
    /// Событие, вызываемое при выходе из управления устройствами.
    /// </summary>
    public event EventHandler ExitEvent;

    /// <summary>
    /// Экземпляр головного устройства.
    /// </summary>
    private IHeadUnit _headUnit;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DeviceManagerControl"/>.
    /// </summary>
    public DeviceManagerControl()
    {
      InitializeComponent();
      BreakdownTesterControl.PlusEvent += (s, a) => AddBreakdownEvent?.Invoke(this, _headUnit);
      SwitchingDeviceControl.PlusEvent += (s, a) => DeviceBusCommutationSelected?.Invoke(this, _headUnit);
      FastMeterControl.PlusEvent += (s, a) => FastMeterEvent?.Invoke(this, _headUnit);
      PowerSourceModuleControl.PlusEvent += (s, a) => PowerModuleEvent?.Invoke(this, _headUnit);
      RelaySwitchModuleControl.PlusEvent += (s, a) => ModuleRelayEvent?.Invoke(this, _headUnit);
    }

    /// <summary>
    /// Устанавливает головное устройство.
    /// </summary>
    /// <typeparam name="T">Тип головного устройства.</typeparam>
    /// <param name="headUnit">Экземпляр головного устройства.</param>
    public void SetHeadUnit<T>(T headUnit) where T : class, IHeadUnit
    {
      _headUnit = headUnit;
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки добавления модуля релейного коммутатора.
    /// </summary>
    private void addModuleRelayButton_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки добавления модуля источника напряжения/тока.
    /// </summary>
    private void addModuleVoltageCurrentSourceButton_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки добавления точного измерителя.
    /// </summary>
    private void addAccurateMeterButton_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки добавления быстрого измерителя.
    /// </summary>
    private void addFastMeterButton_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
    }

    /// <summary>
    /// Обрабатывает выход из управления устройствами.
    /// </summary>
    private void Exit_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      ExitEvent?.Invoke(this, EventArgs.Empty);
    }
  }
}
