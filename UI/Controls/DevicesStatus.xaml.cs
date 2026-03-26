using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using System.Windows.Controls;

namespace UI.Controls
{
  /// <summary>
  /// Логика взаимодействия для DevicesStatus.xaml
  /// </summary>
  public partial class DevicesStatus : UserControl
  {
    public DevicesStatus()
    {
      InitializeComponent();
      UpdateState.PreviewMouseDown += (s, a) => ExecutionEventAdapter.RaiseDeviceStatusUpdate();
    }
    public void AddDevice(IAttachableDevice device)
    {
      Dispatcher.Invoke(() =>
      {
        if (device == null)
          return;

        var deviceState = new DeviceState(device);
        deviceState.LoadData(device);
        RootPanel.Children.Add(deviceState);
      });
    }

    public void LoadDevices(IEnumerable<IAttachableDevice> devices)
    {
      ClearDevices();

      foreach (var device in devices)
      {
        AddDevice(device);
      }
    }

    public void ClearDevices()
    {
      Dispatcher.Invoke(() => RootPanel.Children.Clear());
    }
  }
}
