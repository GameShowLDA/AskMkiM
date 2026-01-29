using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
      Dispatcher.Invoke(() =>RootPanel.Children.Clear());
    }
  }
}
