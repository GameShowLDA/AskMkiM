using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using System.Windows.Controls;

namespace UI.Controls.Settings.DeviceConfig.DeviceManager
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

    public event EventHandler<BreakdownTesterEntity> EditBreakdownEvent;
    public event EventHandler<FastMeterEntity> EditFastMeterEvent;
    public event EventHandler<PowerSourceModuleEntity> EditPowerModuleEvent;
    public event EventHandler<RelaySwitchModuleEntity> EditModuleRelayEvent;
    public event EventHandler<SwitchingDeviceEntity> EditDeviceBusCommutationEvent;

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

      FastMeterControl.IsSingleDeviceOnly = true;
      PrecisionMeterControl.IsSingleDeviceOnly = true;
      BreakdownTesterControl.IsSingleDeviceOnly = true;
      SwitchingDeviceControl.IsSingleDeviceOnly = true;

      BreakdownTesterControl.PlusEvent += (s, a) => AddBreakdownEvent?.Invoke(this, _headUnit);
      SwitchingDeviceControl.PlusEvent += (s, a) => DeviceBusCommutationSelected?.Invoke(this, _headUnit);
      FastMeterControl.PlusEvent += (s, a) => FastMeterEvent?.Invoke(this, _headUnit);
      PowerSourceModuleControl.PlusEvent += (s, a) => PowerModuleEvent?.Invoke(this, _headUnit);
      RelaySwitchModuleControl.PlusEvent += (s, a) => ModuleRelayEvent?.Invoke(this, _headUnit);

      BreakdownTesterControl.EditEvent += (s, device) =>
      {
        if (device is BreakdownTesterEntity entity)
        {
          EditBreakdownEvent?.Invoke(this, entity);
        }
      };

      FastMeterControl.EditEvent += (s, device) =>
      {
        if (device is FastMeterEntity entity)
        {
          EditFastMeterEvent?.Invoke(this, entity);
        }
      };

      PowerSourceModuleControl.EditEvent += (s, device) =>
      {
        if (device is PowerSourceModuleEntity entity)
        {
          EditPowerModuleEvent?.Invoke(this, entity);
        }
      };

      RelaySwitchModuleControl.EditEvent += (s, device) =>
      {
        if (device is RelaySwitchModuleEntity entity)
        {
          EditModuleRelayEvent?.Invoke(this, entity);
        }
      };

      SwitchingDeviceControl.EditEvent += (s, device) =>
      {
        if (device is SwitchingDeviceEntity entity)
        {
          EditDeviceBusCommutationEvent?.Invoke(this, entity);
        }
      };
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
