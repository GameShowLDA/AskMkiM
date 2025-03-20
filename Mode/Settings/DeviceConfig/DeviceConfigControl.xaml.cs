using System.Windows;
using System.Windows.Controls;
using AppConfig.DataBase.Models;
using AppConfig.DataBase.Repositories;
using AppConfig.DataBase.Services;
using Mode.Settings.DeviceConfig.BreakDown;
using Mode.Settings.DeviceConfig.ChassisManager;
using Mode.Settings.DeviceConfig.DeviceBusCommutation;
using Mode.Settings.DeviceConfig.DeviceManager;
using Mode.Settings.DeviceConfig.FastMeter;
using Mode.Settings.DeviceConfig.ModuleRelayControl;
using Mode.Settings.DeviceConfig.ModuleVoltageCurrentSource;
using NewCore.Base.Interface.Additionally;
using static AppConfig.Config.SystemStateManager;

namespace Mode.Settings.DeviceConfig
{
  /// <summary>
  /// Логика взаимодействия для DeviceConfigControl.xaml.
  /// </summary>
  public partial class DeviceConfigControl : UserControl
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DeviceConfigControl"/>.
    /// </summary>
    public DeviceConfigControl()
    {
      InitializeComponent();
      chassisManager.NewSystem += (s, a) => NewSystem();
      chassisManager.SystemSelected += (s, a) => SelectedChassis(a);

      try
      {
        var data = new ChassisManagerRepository().GetAll().First();
        AddSystem(data);
      }
      catch
      {
        return;
      }
    }

    /// <summary>
    /// Устанавливает контрол управления устройствами.
    /// </summary>
    /// <param name="deviceManagerControl">Контрол для управления устройствами.</param>
    public void SetDevisesControl(DeviceManagerControl deviceManagerControl)
    {
      deviceBorder.Child = deviceManagerControl;
    }

    /// <summary>
    /// Обрабатывает выбор шасси.
    /// </summary>
    /// <param name="system">Выбранное шасси.</param>
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
      devices.ModuleRelayEvent += (s,a) => Devices_ModuleRalayEvent(s, a, system, devices);
      devices.FastMeterEvent += (s, a) => Devices_FastMeterEvent(s, a, system, devices);
      devices.ExitEvent += Devices_ExitEvent;
    }

    /// <summary>
    /// Отображает окно быстрого измерителя.
    /// </summary>
    private void Devices_FastMeterEvent(object? sender, IHeadUnit e, ChassisManagerEntity system, DeviceManagerControl devices)
    {
      this.Effect = new System.Windows.Media.Effects.BlurEffect();
      FastMeterWindow fastMeterWindow = new FastMeterWindow();
      fastMeterWindow.SetSettings(sender, e);
      fastMeterWindow.RequestSave += (s, a) => LoadFastMeters(system, devices);
      fastMeterWindow.ShowDialog();
      this.Effect = null;
    }

    /// <summary>
    /// Отображает окно источника напряжения.
    /// </summary>
    private void Devices_PowerModuleEvent(object? sender, IHeadUnit e, ChassisManagerEntity system, DeviceManagerControl devices)
    {
      this.Effect = new System.Windows.Media.Effects.BlurEffect();
      ModuleVoltageCurrentSourceWindow fastMeterWindow = new ModuleVoltageCurrentSourceWindow();
      fastMeterWindow.SetSettings(sender, e);
      fastMeterWindow.RequestSave += (s, a) => LoadPowerSources(system, devices);
      fastMeterWindow.ShowDialog();
      this.Effect = null;
    }

    /// <summary>
    /// Отображает окно источника напряжения.
    /// </summary>
    private void Devices_ModuleRalayEvent(object? sender, IHeadUnit e, ChassisManagerEntity system, DeviceManagerControl devices)
    {
      this.Effect = new System.Windows.Media.Effects.BlurEffect();
      ModuleRelayControlWindow fastMeterWindow = new ModuleRelayControlWindow();
      fastMeterWindow.SetSettings(sender, e);
      fastMeterWindow.RequestSave += (s, a) => LoadRelaySwitchModules(system, devices);
      fastMeterWindow.ShowDialog();
      this.Effect = null;
    }

    /// <summary>
    /// Отображает окно коммутации шин.
    /// </summary>
    private void Devices_DeviceBusCommutationSelected(object? sender, IHeadUnit e, ChassisManagerEntity system, DeviceManagerControl devices)
    {
      this.Effect = new System.Windows.Media.Effects.BlurEffect();
      DeviceBusCommutationWindow deviceSettingsWindow = new DeviceBusCommutationWindow();
      deviceSettingsWindow.SetSettings(sender, e);
      deviceSettingsWindow.RequestSave += (s, a) => LoadSwitchingDevices(system, devices);
      deviceSettingsWindow.ShowDialog();
      this.Effect = null;
    }

    /// <summary>
    /// Обрабатывает выход из управления устройствами.
    /// </summary>
    private void Devices_ExitEvent(object? sender, EventArgs e)
    {
      ToggleThirdColumn(false);
      deviceBorder.Child = null;
      settingsBorder.Child = null;
    }

    /// <summary>
    /// Отображает окно пробойной установки.
    /// </summary>
    private void Devices_AddBreakdownEvent(object? sender, IHeadUnit e, ChassisManagerEntity system, DeviceManagerControl devices)
    {
      this.Effect = new System.Windows.Media.Effects.BlurEffect();
      BreakDownWindow fastMeterWindow = new BreakDownWindow();
      fastMeterWindow.SetSettings(sender, e);
      fastMeterWindow.RequestSave += (s, a) => LoadBreakdownTesters(system, devices);
      fastMeterWindow.ShowDialog();
      this.Effect = null;
    }

    /// <summary>
    /// Показывает или скрывает третью колонку в интерфейсе.
    /// </summary>
    /// <param name="isVisible">Флаг видимости.</param>
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
      devicesControl.ClearDevice<BreakdownTesterEntity>(new BreakdownTesterEntity());

      var breakdownTesters = new BreakdownTesterRepository()
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
      devicesControl.ClearDevice<FastMeterEntity>(new FastMeterEntity());

      var fastMeters = new FastMeterRepository()
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
      devicesControl.ClearDevice<PrecisionMeterEntity>(new PrecisionMeterEntity());

      var precisionMeters = new PrecisionMeterRepository()
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
      devicesControl.ClearDevice<PowerSourceModuleEntity>(new PowerSourceModuleEntity());

      var powerSources = new PowerSourceModuleRepository()
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
      devicesControl.ClearDevice<RelaySwitchModuleEntity>(new RelaySwitchModuleEntity());

      var relaySwitchModules = new RelaySwitchModuleRepository().GetDevicesByNumberChassis(chassis.Number);

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
      devicesControl.ClearDevice<SwitchingDeviceEntity>(new SwitchingDeviceEntity());

      var switchingDevices = new SwitchingDeviceRepository()
          .GetDevicesByNumberChassis(chassis.Number);

      foreach (var device in switchingDevices)
      {
        devicesControl.AddDevice(device);
      }
    }

    /// <summary>
    /// Добавляет систему в список.
    /// </summary>
    /// <param name="data">Данные системы.</param>
    public void AddSystem(ChassisManagerEntity data)
    {
      chassisManager.AddSystem(data);
    }

    /// <summary>
    /// Добавляет стойку в список.
    /// </summary>
    /// <param name="data">Данные стойки.</param>
    public void AddRack(RackEntity data)
    {
      chassisManager.AddRack(data);
    }

    /// <summary>
    /// Создает новое шасси.
    /// </summary>
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

    /// <summary>
    /// Обрабатывает сохранение конфигурации шасси.
    /// </summary>
    private void ChassisManagerSettings_DeviceSaved(object sender, ChassisManagerEntity device)
    {
      deviceBorder.Child = null;
      chassisManager.Visibility = Visibility.Visible;
      if (device == null)
      {
        return;
      }

      chassisManager.AddSystem(device);
    }

    /// <summary>
    /// Обрабатывает закрытие окна настроек.
    /// </summary>
    private void Setting_RequestClose(object? sender, EventArgs e)
    {
      deviceBorder.Child = null;
      chassisManager.Visibility = Visibility.Visible;
    }
  }
}
