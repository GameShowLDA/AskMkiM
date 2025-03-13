using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using AppConfig.DataBase.Services;
using NewCore.Base;
using NewCore.Interface;

namespace Mode.Settings.DeviceConfig.Controls
{
  public partial class DeviceListControl : UserControl
  {
    public event EventHandler PlusEvent;
    public event EventHandler<IDevice> DeleteEvent;

    public static readonly DependencyProperty DeviceTitleProperty =
        DependencyProperty.Register(nameof(DeviceTitle), typeof(string), typeof(DeviceListControl),
        new PropertyMetadata("Название устройства"));

    public string DeviceTitle
    {
      get => (string)GetValue(DeviceTitleProperty);
      set => SetValue(DeviceTitleProperty, value);
    }

    // Коллекция устройств (IDevice + DisplayName)
    public ObservableCollection<DeviceWrapper> Devices { get; set; } = new ObservableCollection<DeviceWrapper>();

    public DeviceListControl()
    {
      InitializeComponent();
      DataContext = this;
      DeviceList.ItemsSource = Devices;
    }

    // Метод добавления устройства
    public void AddDevice(IDevice device)
    {
      Devices.Add(new DeviceWrapper(device));
    }

    // Метод удаления устройства
    public void RemoveDevice(DeviceWrapper deviceWrapper)
    {
      if (Devices.Contains(deviceWrapper))
      {
        Devices.Remove(deviceWrapper);
        Type deviceType = deviceWrapper.Device.GetType();
        RemoveDeviceFromDatabase(deviceWrapper.Device);

        DeleteEvent?.Invoke(this, deviceWrapper.Device);
      }
    }

    private void RemoveDeviceFromDatabase(IDevice device)
    {
      switch (device)
      {
        case ISwitchingDevice:
          new SwitchingDeviceRepository(AppConfig.Config.SystemStateManager.Context).Delete(device.Id);
          Utilities.LoggerUtility.LogInformation("Удаляем устройство из SwitchingDeviceTable");
          break;

        case IFastMeter:
          new FastMeterRepository(AppConfig.Config.SystemStateManager.Context).Delete(device.Id);
          Utilities.LoggerUtility.LogInformation("Удаляем устройство из FastMeterRepository");
          break;

        default:
          Utilities.LoggerUtility.LogInformation($"Неизвестный интерфейс {device.GetType().Name}, удаление не выполнено");
          throw new NotFiniteNumberException();
      }
    }



    private void RemoveDeviceButton_Click(object sender, RoutedEventArgs e)
    {
      if (sender is Button button && button.CommandParameter is DeviceWrapper deviceWrapper)
      {
        IDevice device = deviceWrapper.Device; // Получаем IDevice
        RemoveDevice(deviceWrapper);
      }
    }


    private void PlusPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      PlusEvent?.Invoke(this, e);
    }
  }

  public class DeviceWrapper
  {
    public IDevice Device { get; }

    public string DisplayName => $"{Device.Name} ({Device.Number})";

    public DeviceWrapper(IDevice device)
    {
      Device = device;
    }

    public override string ToString()
    {
      return DisplayName;
    }
  }

}
