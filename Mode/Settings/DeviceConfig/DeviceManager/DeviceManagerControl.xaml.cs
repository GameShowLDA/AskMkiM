using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UI.Components;

namespace Mode.Settings.DeviceConfig.DeviceManager
{
  /// <summary>
  /// Логика взаимодействия для DeviceManagerControl.xaml
  /// </summary>
  public partial class DeviceManagerControl : UserControl
  {
    public event EventHandler AddBreakdownEvent;
    public event EventHandler ExitEvent;
    public event EventHandler DeviceBusCommutationSelected;
    public DeviceManagerControl()
    {
      InitializeComponent();
    }

    private void PlusButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
      ((PlusButtonControl)sender).Foreground = (Brush)Application.Current.Resources["ActiveForegroundSolidColorBrush"];
    }
    private void PlusButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
      ((PlusButtonControl)sender).Foreground = (Brush)Application.Current.Resources["ForegroundSolidColorBrush"];
    }

    private void addDeviceBusCommutationButton_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      DeviceBusCommutationSelected?.Invoke(sender, e);
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

    private void addBrakedownButton_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      AddBreakdownEvent?.Invoke(this, EventArgs.Empty);
    }

    private void Exit_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      ExitEvent?.Invoke(this, EventArgs.Empty);
    }
  }
}
