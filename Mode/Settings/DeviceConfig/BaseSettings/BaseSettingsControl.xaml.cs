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

namespace Mode.Settings.DeviceConfig.BaseSettings
{
  /// <summary>
  /// Логика взаимодействия для BaseSettingsControl.xaml
  /// </summary>
  public partial class BaseSettingsControl : UserControl
  {
    public event EventHandler RequestClose;
    public event EventHandler<ChassisManagerEntity> DeviceSaved;
    private Dictionary<string, Type> deviceModelMap = new Dictionary<string, Type>();
    public BaseSettingsControl()
    {
      InitializeComponent();
    }
  }
}
