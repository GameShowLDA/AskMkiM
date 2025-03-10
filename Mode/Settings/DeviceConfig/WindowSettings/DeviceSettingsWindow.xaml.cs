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
using AppConfig.DataBase.Services;
using NewCore.Base;
using NewCore.Device;
using NewCore.Interface;

namespace Mode.Settings.DeviceConfig.WindowSettings
{
  /// <summary>
  /// Логика взаимодействия для DeviceSettingsWindow.xaml
  /// </summary>
  public partial class DeviceSettingsWindow : Window
  {
    public DeviceSettingsWindow()
    {
      InitializeComponent();
      // deviceSettingsWindow.NameDevice = "Устройство коммутации шин";
      // deviceSettingsWindow.LoadDeviceModels<ISwitchingDevice>();
      // deviceSettingsWindow.SetHeadUnit(e);
      // deviceSettingsWindow.SaveEvent += (s, a) =>
      // {
      //   deviceSettingsWindow.GetDevice<ISwitchingDevice>();
      //   deviceSettingsWindow.Close();
      // };
    }
  }
}
