using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using System.Windows;
using System.Windows.Controls;

namespace UI.Controls
{
  /// <summary>
  /// Логика взаимодействия для DeviceState.xaml
  /// </summary>
  public partial class DeviceState : UserControl
  {
    private IAttachableDevice _device;
    public DeviceState()
    {
      InitializeComponent();
      EventAggregator.Subscribe<ExecutionEvents.DeviceStatusUpdate>(e => LoadData(_device));
    }

    public DeviceState(IAttachableDevice device) : this()
    {
      _device = device;
    }

    // Описание
    public static readonly DependencyProperty DeviceStatusProperty =
      DependencyProperty.Register(nameof(DeviceStatus), typeof(string), typeof(DeviceState), new PropertyMetadata("Оборудование не загружено"));

    public string DeviceStatus
    {
      get => (string)GetValue(DeviceStatusProperty);
      set => SetValue(DeviceStatusProperty, value);
    }

    public void SetDevice(IAttachableDevice device)
    {
      _device = device;
    }

    internal void LoadData(IAttachableDevice device)
    {
      if (device != null && device == _device)
      {
        Dispatcher.Invoke(() =>
        {
          string header = $"{device.Name} {device.NumberChassis}.{device.Number}";
          Header.Text = header;

          try
          {
            var str = device.ConnectableManager.GetConnectionStatus();
            DeviceStatus = str;
          }
          catch (Exception)
          {
            throw;
          }
        });
      }
    }
  }
}
