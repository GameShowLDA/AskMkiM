using System.Windows;
using System.Windows.Controls;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Services;
using Mode.Settings.DeviceConfig.BreakDown;
using Mode.Settings.DeviceConfig.ChassisManager;
using Mode.Settings.DeviceConfig.DeviceBusCommutation;
using Mode.Settings.DeviceConfig.DeviceManager;
using Mode.Settings.DeviceConfig.FastMeter;
using Mode.Settings.DeviceConfig.ModuleVoltageCurrentSource;
using NewCore.Base.Interface.Additionally;
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
      chassisManager.NewSystem += (s, a) => NewSystem();
      chassisManager.SystemSelected += (s, a) => SelectedChassis(a);

      try
      {
        var data = new ChassisManagerRepository(Context).GetAll().First();
        AddSystem(data);
      }
      catch
      {

      }
    }

    public void SetDevisesControl(DeviceManagerControl deviceManagerControl)
    {
      deviceBorder.Child = deviceManagerControl;
    }
    private void SelectedChassis(ChassisManagerEntity system)
    {
      var devices = new DeviceManagerControl();
      devices.SetHeadUnit(system);

      LoadBreakdownTesters(system, devices);
      LoadFastMeters(system, devices);
      LoadPrecisionMeters(system, devices);
      LoadPowerSources(system, devices);
      LoadRelaySwitchModules(system, devices);
      LoadSwitchingDevices(system, devices);
      deviceBorder.Child = devices;

      devices.AddBreakdownEvent += (s, a) => Devices_AddBreakdownEvent(s, a, system, devices);
      devices.DeviceBusCommutationSelected += (s, a) => Devices_DeviceBusCommutationSelected(s, a, system, devices);
      devices.PowerModuleEvent += (s, a) => Devices_PowerModuleEvent(s, a, system, devices);
      devices.FastMeterEvent += (s, a) => Devices_FastMeterEvent(s, a, system, devices);
      devices.ExitEvent += Devices_ExitEvent;
    }

    private void Devices_FastMeterEvent(object? sender, IHeadUnit e, ChassisManagerEntity system, DeviceManagerControl devices)
    {
      this.Effect = new System.Windows.Media.Effects.BlurEffect();
      FastMeterWindow fastMeterWindow = new FastMeterWindow();
      fastMeterWindow.SetSettings(sender, e);
      fastMeterWindow.RequestSave += (s, a) => LoadFastMeters(system, devices);
      fastMeterWindow.ShowDialog();
      this.Effect = null;
    }

    private void Devices_PowerModuleEvent(object? sender, IHeadUnit e, ChassisManagerEntity system, DeviceManagerControl devices)
    {
      this.Effect = new System.Windows.Media.Effects.BlurEffect();
      ModuleVoltageCurrentSourceWindow fastMeterWindow = new ModuleVoltageCurrentSourceWindow();
      fastMeterWindow.SetSettings(sender, e);
      fastMeterWindow.RequestSave += (s, a) => LoadPowerSources(system, devices);
      fastMeterWindow.ShowDialog();
      this.Effect = null;
    }

    private void Devices_DeviceBusCommutationSelected(object? sender, IHeadUnit e, ChassisManagerEntity system, DeviceManagerControl devices)
    {
      this.Effect = new System.Windows.Media.Effects.BlurEffect();
      DeviceBusCommutationWindow deviceSettingsWindow = new DeviceBusCommutationWindow();
      deviceSettingsWindow.SetSettings(sender, e);
      deviceSettingsWindow.RequestSave += (s, a) => LoadSwitchingDevices(system, devices);
      deviceSettingsWindow.ShowDialog();
      this.Effect = null;
    }

    private void DbcControl_RequestClose(object? sender, EventArgs e)
    {
      ToggleThirdColumn(false);
      settingsBorder.Child = null;
    }

    private void Devices_ExitEvent(object? sender, EventArgs e)
    {
      ToggleThirdColumn(false);
      deviceBorder.Child = null;
      settingsBorder.Child = null;
    }

    private void Devices_AddBreakdownEvent(object? sender, IHeadUnit e, ChassisManagerEntity system, DeviceManagerControl devices)
    {
      this.Effect = new System.Windows.Media.Effects.BlurEffect();
      BreakDownWindow fastMeterWindow = new BreakDownWindow();
      fastMeterWindow.SetSettings(sender, e);
      fastMeterWindow.RequestSave += (s, a) => LoadBreakdownTesters(system, devices);
      fastMeterWindow.ShowDialog();
      this.Effect = null;
    }

    private void BreakDownControl_DeviceSaved(object? sender, EventArgs e)
    {
      ToggleThirdColumn(false);
      settingsBorder.Child = null;
      MessageBox.Show("Пробойная установка добавлена в конфигурацию!");
    }

    private void BreakDownControl_ClosedEvent(object? sender, EventArgs e)
    {
      ToggleThirdColumn(false);
      settingsBorder.Child = null;
    }

    public void ToggleThirdColumn(bool isVisible)
    {
      if (isVisible)
      {
        Column1.Width = new GridLength(0);
        Column3.Width = new GridLength(1, GridUnitType.Star);
        settingsBorder.Visibility = Visibility.Visible;
      }
      else
      {
        Column1.Width = new GridLength(1, GridUnitType.Auto);
        Column3.Width = new GridLength(0);
        settingsBorder.Visibility = Visibility.Collapsed;
      }
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

    public void AddRack(RackEntity data)
    {
      chassisManager.AddRack(data);
    }

    private void NewSystem()
    {
      ChassisManagerWindow chassisManagerWindow = new ChassisManagerWindow();
      chassisManagerWindow.SetSettings();
      chassisManagerWindow.RequestClose += Setting_RequestClose;
      chassisManagerWindow.RequestSave += ChassisManagerSettings_DeviceSaved;

      this.Effect = new System.Windows.Media.Effects.BlurEffect();
      chassisManagerWindow.ShowDialog();
      this.Effect = null;
    }

    private void ChassisManager_NewRack(object? sender, EventArgs e)
    {
      //var setting = new RackSettings
      //{
      //  HorizontalAlignment = HorizontalAlignment.Stretch,
      //  VerticalAlignment = VerticalAlignment.Stretch,
      //  Margin = new Thickness(0),
      //  Width = Double.NaN,    // Автоматическая ширина
      //  Height = Double.NaN,    // Автоматическая высота
      //};

      //setting.RequestClose += Setting_RequestClose;
      //setting.RequestSave += Setting_RequestSave; ;

      //deviceBorder.Child = setting;
      //deviceBorder.UpdateLayout();
      //setting.UpdateLayout();
      //chassisManager.Visibility = Visibility.Collapsed;
      //settingsBorder.Visibility = Visibility.Collapsed;
    }

    private void Setting_RequestSave(object? sender, RackEntity device)
    {
      deviceBorder.Child = null;
      chassisManager.Visibility = Visibility.Visible;
      chassisManager.AddRack(device);
    }

    private void ChassisManagerSettings_DeviceSaved(object sender, ChassisManagerEntity device)
    {
      // TODO : Добавить в список устройств
      deviceBorder.Child = null;
      chassisManager.Visibility = Visibility.Visible;
      if (device == null)
      {
        return;
      }

      chassisManager.AddSystem(device);
    }

    private void Setting_RequestClose(object? sender, EventArgs e)
    {
      deviceBorder.Child = null;
      chassisManager.Visibility = Visibility.Visible;
    }
  }
}
