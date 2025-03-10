using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Net;
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
using AppConfig.DataBase.Services;
using Microsoft.IdentityModel.Tokens;
using Mode.Settings.DeviceConfig.Base;
using NewCore.Base;
using NewCore.Device;
using NewCore.Interface;

namespace Mode.Settings.DeviceConfig.ChassisManager
{
  /// <summary>
  /// Логика взаимодействия для RackSettings.xaml
  /// </summary>
  public partial class RackSettings : UserControl, IDataProcessor
  {
    /// <summary>
    /// Событие, вызываемое при запросе на закрытие окна настроек.
    /// Может использоваться для обработки логики отмены или сохранения данных перед закрытием.
    /// </summary>
    public event EventHandler RequestClose;

    /// <summary>
    /// Событие, вызываемое при запросе на сохранение настроек.
    /// Может использоваться для обработки логики отмены или сохранения данных перед закрытием.
    /// </summary>
    public event EventHandler<RackEntity> RequestSave;

    public RackSettings()
    {
      InitializeComponent();
    }

    public bool HandleData(object instance)
    {
      throw new NotImplementedException();
    }
  }
}
