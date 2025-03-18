using System.Windows.Controls;
using NewCore.Base.Interface.Additionally;

namespace Mode.Settings.DeviceConfig.DeviceManager
{
  /// <summary>
  /// Логика взаимодействия для DeviceManagerControl.xaml
  /// </summary>
  public partial class DeviceManagerControl : UserControl
  {
    public event EventHandler<IHeadUnit> AddBreakdownEvent;
    public event EventHandler<IHeadUnit> DeviceBusCommutationSelected;
    public event EventHandler<IHeadUnit> FastMeterEvent;
    public event EventHandler<IHeadUnit> PowerModuleEvent;
    public event EventHandler ExitEvent;
    public DeviceManagerControl()
    {
      InitializeComponent();
      BreakdownTesterControl.PlusEvent += (s, a) => AddBreakdownEvent?.Invoke(this, _headUnit);
      SwitchingDeviceControl.PlusEvent += (s, a) => DeviceBusCommutationSelected?.Invoke(this, _headUnit);
      FastMeterControl.PlusEvent += (s, a) => FastMeterEvent?.Invoke(this, _headUnit);
      PowerSourceModuleControl.PlusEvent += (s, a) => PowerModuleEvent?.Invoke(this, _headUnit);
    }
    private IHeadUnit _headUnit;


    public void SetHeadUnit<T>(T headUnit) where T : class, IHeadUnit
    {
      _headUnit = headUnit;
    }

    private void addModuleRelayButton_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {

    }

    private void addModuleVoltageCurrentSourceButton_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {

    }

    private void addAccurateMeterButton_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {

    }

    private void addFastMeterButton_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {

    }

    private void Exit_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      ExitEvent?.Invoke(this, EventArgs.Empty);
    }
  }
}
