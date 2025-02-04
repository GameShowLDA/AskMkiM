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
using AppConfig.DataBase.Models;
using Mode.Settings.DeviceConfig.DeviceManager;
using NewCore.Interface;

namespace Mode.Settings.DeviceConfig
{
  /// <summary>
  /// Логика взаимодействия для DeviceConfigControl.xaml
  /// </summary>
  public partial class DeviceConfigControl : UserControl
  {
    public DeviceConfigControl()
    {
      InitializeComponent();
    }

    public void SetDevisesControl(DeviceManagerControl deviceManagerControl)
    {
      deviceBorder.Child = deviceManagerControl;
    }

    public void AddSystem(ChassisManagerEntity data) => chassisManager.AddSystem(data);
  }
}
