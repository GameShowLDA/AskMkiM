using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace UI.Controls.DeviceHealthView
{
  public partial class DeviceHealthViewControl : UserControl
  {
    public ObservableCollection<DeviceWrapper> Devices { get; } = new();

    public DeviceHealthViewControl()
    {
      InitializeComponent();
      DevicesList.ItemsSource = Devices;
    }

    public void Add(IDevice device)
    {
      Devices.Add(new DeviceWrapper(device));
    }

    public void AddRange(List<IDevice> devices)
    {
      foreach (var device in devices)
      {
        Devices.Add(new DeviceWrapper(device));
      }
    }

    public void Clear()
    {
      Devices.Clear();
    }

    private void DevicePreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (sender is Button btn && btn.Tag is DeviceWrapper wrapper)
      {
        IDevice device = wrapper.Device;
        Header.Text = $"{device.Name} ({device.Number})";
        HandleDeviceClick(device);
      }
    }

    private void HandleDeviceClick(IDevice device)
    {
      var str = device.ConnectableManager.GetConnectionStatus();
      Desription.Text = str;
    }
  }

  public class DeviceWrapper
  {
    public IDevice Device { get; }
    public string NameNumber => $"{Device.Name} ({Device.Number})";

    public DeviceWrapper(IDevice device)
    {
      Device = device;
    }
  }
}
