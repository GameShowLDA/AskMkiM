using System.IO.Ports;
using System.Management;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Services;
using Mode.Settings.DeviceConfig.Base;
using NewCore.Base;
using NewCore.Device;
using NewCore.Interface;

namespace Mode.Settings.DeviceConfig.ChassisManager
{
  /// <summary>
  /// Представляет элемент управления для настройки устройств ChassisManager.
  /// </summary>
  public partial class ChassisManagerSettings : UserControl, IDataProcessor
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
    public event EventHandler<ChassisManagerEntity> RequestSave;

    /// <summary>
    /// Конструктор класса ChassisManagerSettings. 
    /// Инициализирует элементы управления и загружает модели устройств.
    /// </summary>
    public ChassisManagerSettings()
    {
      InitializeComponent();
    }

    public bool HandleData(object instance)
    {
      throw new NotImplementedException();
    }
  }
}
