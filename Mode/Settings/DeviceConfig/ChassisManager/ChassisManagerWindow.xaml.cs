using AppConfig.DataBase.Models;
using Mode.Settings.DeviceConfig.Base.BaseSettingsConfig;
using NewCore.Interface;
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

namespace Mode.Settings.DeviceConfig.ChassisManager
{
  /// <summary>
  /// Логика взаимодействия для ChassisManagerWindow.xaml
  /// </summary>
  public partial class ChassisManagerWindow : Window
  {
    public EventHandler RequestClose;
    public EventHandler<ChassisManagerEntity> RequestSave;
    public ChassisManagerWindow()
    {
      InitializeComponent();
    }

    public DeviceBase Property => new DeviceBase(deviceSettingsWindow);
    public void SetSettings()
    {
      deviceSettingsWindow.NameDevice = "Тест АСКМ";
      deviceSettingsWindow.LoadDeviceModels<IChassisManager>();
      deviceSettingsWindow.SaveEvent += (s, a) =>
      {
        RequestSave?.Invoke(s, null);
        this.Close();
      };

      deviceSettingsWindow.RequestClose += (s, a) =>
      {
        RequestClose?.Invoke(s, a);
        this.Close();
      };
    }
  }
}
