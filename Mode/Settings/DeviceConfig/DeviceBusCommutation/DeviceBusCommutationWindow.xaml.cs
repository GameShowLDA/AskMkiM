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
using System.Windows.Shapes;
using NewCore.Interface;

namespace Mode.Settings.DeviceConfig.DeviceBusCommutation
{
  /// <summary>
  /// Логика взаимодействия для DeviceBusCommutationWindow.xaml
  /// </summary>
  public partial class DeviceBusCommutationWindow : Window
  {
    public DeviceBusCommutationWindow()
    {
      InitializeComponent();
    }

    public void SetSettings(object? sender, IHeadUnit e)
    {
      deviceSettingsWindow.NameDevice = "Устройство коммутации шин";
      deviceSettingsWindow.LoadDeviceModels<ISwitchingDevice>();
      deviceSettingsWindow.SetHeadUnit(e);
      deviceSettingsWindow.SaveEvent += (s, a) =>
      {
        deviceSettingsWindow.GetDevice<ISwitchingDevice>();
        this.Close();
      };
    }
  }
}
