using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using NewCore.Base;

namespace Mode.Settings.DeviceConfig.Controls
{
  public partial class DeviceListControl : UserControl
  {
    public EventHandler PlusEvent;
    public EventHandler DeleteEvent;

    public static readonly DependencyProperty DeviceTitleProperty =
        DependencyProperty.Register(nameof(DeviceTitle), typeof(string), typeof(DeviceListControl),
        new PropertyMetadata("Название устройства"));

    public string DeviceTitle
    {
      get => (string)GetValue(DeviceTitleProperty);
      set => SetValue(DeviceTitleProperty, value);
    }

    // Коллекция устройств
    public ObservableCollection<string> Devices { get; set; } = new ObservableCollection<string>();

    public DeviceListControl()
    {
      InitializeComponent();
      DataContext = this;
      DeviceList.ItemsSource = Devices;
    }

    // Метод добавления устройства
    public void AddDevice(IDevice device)
    {
      int newDeviceNumber = Devices.Count + 1;

      Devices.Add($"{device.Name} {device.Number}");
    }

    // Метод удаления устройства
    public void RemoveDevice(string deviceName)
    {
      if (Devices.Contains(deviceName))
      {
        Devices.Remove(deviceName);
        DeleteEvent?.Invoke(this, EventArgs.Empty);
      }
    }

    private void RemoveDeviceButton_Click(object sender, RoutedEventArgs e)
    {
      if (sender is Button button && button.DataContext is string deviceName)
      {
        RemoveDevice(deviceName);
      }
    }

    private void PlusPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      PlusEvent?.Invoke(this, e);
    }
  }
}
