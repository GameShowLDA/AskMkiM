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
using AppConfig.DataBase;
using AppConfig.DataBase.Models;
using Microsoft.EntityFrameworkCore;
using Mode.Settings.DeviceConfig.DeviceManager;
using NewCore.Interface;
using static AppConfig.Config.SystemStateManager;

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
      chassisManager.SystemSelected += (sender, system) => SelectedChassis(system);
    }

    public void SetDevisesControl(DeviceManagerControl deviceManagerControl)
    {
      deviceBorder.Child = deviceManagerControl;
    }

    private void SelectedChassis(ChassisManagerEntity system)
    {
      var devices = new DeviceManagerControl();
      LoadBreakdownTesters(system, devices);
      LoadFastMeters(system, devices);
      LoadPrecisionMeters(system, devices);
      LoadPowerSources(system, devices);
      LoadRelaySwitchModules(system, devices);
      LoadSwitchingDevices(system, devices);
      deviceBorder.Child = devices;
    }

    /// <summary>
    /// Загружает все пробойные установки, привязанные к указанному шасси, и добавляет их в контрол управления устройствами.
    /// </summary>
    /// <param name="chassis">Менеджер шасси.</param>
    /// <param name="devicesControl">Контрол для отображения устройств.</param>
    private void LoadBreakdownTesters(ChassisManagerEntity chassis, DeviceManagerControl devicesControl)
    {
      var breakdownTesters = new AppConfig.DataBase.Services.BreakdownTesterRepository(Context)
          .GetDevicesByNumberChassis(chassis.Number);

      foreach (var device in breakdownTesters)
      {
        devicesControl.AddDevice(device);
      }
    }

    /// <summary>
    /// Загружает все быстрые измерители, привязанные к указанному шасси, и добавляет их в контрол управления устройствами.
    /// </summary>
    /// <param name="chassis">Менеджер шасси.</param>
    /// <param name="devicesControl">Контрол для отображения устройств.</param>
    private void LoadFastMeters(ChassisManagerEntity chassis, DeviceManagerControl devicesControl)
    {
      var fastMeters = new AppConfig.DataBase.Services.FastMeterRepository(Context)
          .GetDevicesByNumberChassis(chassis.Number);

      foreach (var device in fastMeters)
      {
        devicesControl.AddDevice(device);
      }
    }

    /// <summary>
    /// Загружает все точные измерители, привязанные к указанному шасси, и добавляет их в контрол управления устройствами.
    /// </summary>
    /// <param name="chassis">Менеджер шасси.</param>
    /// <param name="devicesControl">Контрол для отображения устройств.</param>
    private void LoadPrecisionMeters(ChassisManagerEntity chassis, DeviceManagerControl devicesControl)
    {
      var precisionMeters = new AppConfig.DataBase.Services.PrecisionMeterRepository(Context)
          .GetDevicesByNumberChassis(chassis.Number);

      foreach (var device in precisionMeters)
      {
        devicesControl.AddDevice(device);
      }
    }

    /// <summary>
    /// Загружает все модули источников питания, привязанные к указанному шасси, и добавляет их в контрол управления устройствами.
    /// </summary>
    /// <param name="chassis">Менеджер шасси.</param>
    /// <param name="devicesControl">Контрол для отображения устройств.</param>
    private void LoadPowerSources(ChassisManagerEntity chassis, DeviceManagerControl devicesControl)
    {
      var powerSources = new AppConfig.DataBase.Services.PowerSourceModuleRepository(Context)
          .GetDevicesByNumberChassis(chassis.Number);

      foreach (var device in powerSources)
      {
        devicesControl.AddDevice(device);
      }
    }

    /// <summary>
    /// Загружает все модули релейных коммутаторов, привязанные к указанному шасси, и добавляет их в контрол управления устройствами.
    /// </summary>
    /// <param name="chassis">Менеджер шасси.</param>
    /// <param name="devicesControl">Контрол для отображения устройств.</param>
    private void LoadRelaySwitchModules(ChassisManagerEntity chassis, DeviceManagerControl devicesControl)
    {
      var relaySwitchModules = new AppConfig.DataBase.Services.RelaySwitchModuleRepository(Context)
          .GetDevicesByNumberChassis(chassis.Number);

      foreach (var device in relaySwitchModules)
      {
        devicesControl.AddDevice(device);
      }
    }

    /// <summary>
    /// Загружает все устройства коммутации, привязанные к указанному шасси, и добавляет их в контрол управления устройствами.
    /// </summary>
    /// <param name="chassis">Менеджер шасси.</param>
    /// <param name="devicesControl">Контрол для отображения устройств.</param>
    private void LoadSwitchingDevices(ChassisManagerEntity chassis, DeviceManagerControl devicesControl)
    {
      var switchingDevices = new AppConfig.DataBase.Services.SwitchingDeviceRepository(Context)
          .GetDevicesByNumberChassis(chassis.Number);

      foreach (var device in switchingDevices)
      {
        devicesControl.AddDevice(device);
      }
    }


    public void AddSystem(ChassisManagerEntity data)
    {
      chassisManager.AddSystem(data);
    }
  }
}
