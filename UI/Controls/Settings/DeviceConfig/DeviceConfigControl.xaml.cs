using Ask.Core.Services.App;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.UI.Infrastructure.UI.Overlay.Drawer.Runtime;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Support;
using DataBaseConfiguration.Services.Device;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using UI.Controls.Settings.DeviceConfig.BreakDown;
using UI.Controls.Settings.DeviceConfig.ChassisManager;
using UI.Controls.Settings.DeviceConfig.DeviceBusCommutation;
using UI.Controls.Settings.DeviceConfig.DeviceManager;
using UI.Controls.Settings.DeviceConfig.FastMeter;
using UI.Controls.Settings.DeviceConfig.ModuleRelayControl;
using UI.Controls.Settings.DeviceConfig.ModuleVoltageCurrentSource;
using UI.Controls.Settings.DeviceConfig.UninterruptiblePowerSupply;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Rack;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;

namespace UI.Controls.Settings.DeviceConfig
{
  /// <summary>
  /// Логика взаимодействия для DeviceConfigControl.xaml.
  /// </summary>
  public partial class DeviceConfigControl : UserControl
  {
    private const double DeviceConfigDrawerPanelWidth = 470d;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DeviceConfigControl"/>.
    /// </summary>
    public DeviceConfigControl()
    {
      InitializeComponent();
      chassisManager.NewSystem += (s, a) => NewSystem();
      chassisManager.SystemSelected += (s, a) => SelectedChassis(a);

      ReloadConfiguration();

      MouseMove += (s, e) =>
      {
        HelpProvider.SetHelpKey(this, "SettingsConfiguration");
      };
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
    private void SelectedChassis(IChassisManager system)
    {
      var devices = new DeviceManagerControl();
      devices.SetHeadUnit(system);

      LoadBreakdownTesters(system, devices);
      LoadFastMeters(system, devices);
      LoadPowerSources(system, devices);
      LoadRelaySwitchModules(system, devices);
      LoadSwitchingDevices(system, devices);
      LoadUninterruptiblePowerSupplies(system, devices);
      deviceBorder.Child = devices;

      devices.AddBreakdownEvent += (s, a) => Devices_AddBreakdownEvent(s, a, system, devices);
      devices.DeviceBusCommutationSelected += (s, a) => Devices_DeviceBusCommutationSelected(s, a, system, devices);
      devices.PowerModuleEvent += (s, a) => Devices_PowerModuleEvent(s, a, system, devices);
      devices.ModuleRelayEvent += (s, a) => Devices_ModuleRalayEvent(s, a, system, devices);
      devices.FastMeterEvent += (s, a) => Devices_FastMeterEvent(s, a, system, devices);
      devices.EditBreakdownEvent += (s, a) => Devices_EditBreakdownEvent(system, devices, a);
      devices.EditDeviceBusCommutationEvent += (s, a) => Devices_EditSwitchingEvent(system, devices, a);
      devices.EditPowerModuleEvent += (s, a) => Devices_EditPowerModuleEvent(system, devices, a);
      devices.EditModuleRelayEvent += (s, a) => Devices_EditRelayEvent(system, devices, a);
      devices.EditFastMeterEvent += (s, a) => Devices_EditFastMeterEvent(system, devices, a);
      devices.UninterruptiblePowerSupplyEvent += (s, a) => Devices_UninterruptiblePowerSupplyEvent(s, a, system, devices);
      devices.EditUninterruptiblePowerSupplyEvent += (s, a) => Devices_EditUninterruptiblePowerSupplyEvent(system, devices, a);
      devices.ExitEvent += Devices_ExitEvent;
    }

    /// <summary>
    /// Отображает окно быстрого измерителя.
    /// </summary>
    private async void Devices_FastMeterEvent(object? sender, IHeadUnit e, IChassisManager system, DeviceManagerControl devices)
    {
      FastMeterWindow fastMeterWindow = new FastMeterWindow();
      fastMeterWindow.SetSettings(sender, e);
      fastMeterWindow.RequestSave += (s, a) => LoadFastMeters(system, devices);
      await OpenWindowInDrawerAsync(fastMeterWindow, "Добавление устройства", "F4 - закрыть");
    }

    /// <summary>
    /// Отображает окно источника напряжения.
    /// </summary>
    private async void Devices_PowerModuleEvent(object? sender, IHeadUnit e, IChassisManager system, DeviceManagerControl devices)
    {
      ModuleVoltageCurrentSourceWindow fastMeterWindow = new ModuleVoltageCurrentSourceWindow();
      fastMeterWindow.SetSettings(sender, e);
      fastMeterWindow.RequestSave += (s, a) => LoadPowerSources(system, devices);
      await OpenWindowInDrawerAsync(fastMeterWindow, "Добавление устройства", "F4 - закрыть");
    }

    /// <summary>
    /// Отображает окно источника напряжения.
    /// </summary>
    private async void Devices_ModuleRalayEvent(object? sender, IHeadUnit e, IChassisManager system, DeviceManagerControl devices)
    {
      ModuleRelayControlWindow fastMeterWindow = new ModuleRelayControlWindow();
      fastMeterWindow.SetSettings(sender, e);
      fastMeterWindow.RequestSave += (s, a) => LoadRelaySwitchModules(system, devices);
      await OpenWindowInDrawerAsync(fastMeterWindow, "Добавление устройства", "F4 - закрыть");
    }

    /// <summary>
    /// Отображает окно коммутации шин.
    /// </summary>
    private async void Devices_DeviceBusCommutationSelected(object? sender, IHeadUnit e, IChassisManager system, DeviceManagerControl devices)
    {
      DeviceBusCommutationWindow deviceSettingsWindow = new DeviceBusCommutationWindow();
      deviceSettingsWindow.SetSettings(sender, e);
      deviceSettingsWindow.RequestSave += (s, a) => LoadSwitchingDevices(system, devices);
      await OpenWindowInDrawerAsync(deviceSettingsWindow, "Добавление устройства", "F4 - закрыть");
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
    private async void Devices_AddBreakdownEvent(object? sender, IHeadUnit e, IChassisManager system, DeviceManagerControl devices)
    {
      BreakDownWindow fastMeterWindow = new BreakDownWindow();
      fastMeterWindow.SetSettings(sender, e);
      fastMeterWindow.RequestSave += (s, a) => LoadBreakdownTesters(system, devices);
      await OpenWindowInDrawerAsync(fastMeterWindow, "Добавление устройства", "F4 - закрыть");
    }

    private async void Devices_EditBreakdownEvent(IChassisManager system, DeviceManagerControl devices, BreakdownTesterEntity entity)
    {
      BreakDownWindow window = new BreakDownWindow();
      window.SetSettings(this, system, entity);
      window.RequestSave += (s, a) => LoadBreakdownTesters(system, devices);
      await OpenWindowInDrawerAsync(window, "Редактирование устройства", "F4 - закрыть");
    }

    private async void Devices_EditSwitchingEvent(IChassisManager system, DeviceManagerControl devices, SwitchingDeviceEntity entity)
    {
      DeviceBusCommutationWindow window = new DeviceBusCommutationWindow();
      window.SetSettings(this, system, entity);
      window.RequestSave += (s, a) => LoadSwitchingDevices(system, devices);
      await OpenWindowInDrawerAsync(window, "Редактирование устройства", "F4 - закрыть");
    }

    private async void Devices_EditPowerModuleEvent(IChassisManager system, DeviceManagerControl devices, PowerSourceModuleEntity entity)
    {
      ModuleVoltageCurrentSourceWindow window = new ModuleVoltageCurrentSourceWindow();
      window.SetSettings(this, system, entity);
      window.RequestSave += (s, a) => LoadPowerSources(system, devices);
      await OpenWindowInDrawerAsync(window, "Редактирование устройства", "F4 - закрыть");
    }

    private async void Devices_EditRelayEvent(IChassisManager system, DeviceManagerControl devices, RelaySwitchModuleEntity entity)
    {
      ModuleRelayControlWindow window = new ModuleRelayControlWindow();
      window.SetSettings(this, system, entity);
      window.RequestSave += (s, a) => LoadRelaySwitchModules(system, devices);
      await OpenWindowInDrawerAsync(window, "Редактирование устройства", "F4 - закрыть");
    }

    private async void Devices_EditFastMeterEvent(IChassisManager system, DeviceManagerControl devices, FastMeterEntity entity)
    {
      FastMeterWindow window = new FastMeterWindow();
      window.SetSettings(this, system, entity);
      window.RequestSave += (s, a) => LoadFastMeters(system, devices);
      await OpenWindowInDrawerAsync(window, "Редактирование устройства", "F4 - закрыть");
    }

    private async void Devices_UninterruptiblePowerSupplyEvent(object? sender, IHeadUnit e, IChassisManager system, DeviceManagerControl devices)
    {
      UninterruptiblePowerSupplyWindow window = new UninterruptiblePowerSupplyWindow();
      window.SetSettings(sender, e);
      window.RequestSave += (s, a) => LoadUninterruptiblePowerSupplies(system, devices);
      await OpenWindowInDrawerAsync(window, "Добавление устройства", "F4 - закрыть");
    }

    private async void Devices_EditUninterruptiblePowerSupplyEvent(IChassisManager system, DeviceManagerControl devices, UninterruptiblePowerSupplyEntity entity)
    {
      UninterruptiblePowerSupplyWindow window = new UninterruptiblePowerSupplyWindow();
      window.SetSettings(this, system, entity);
      window.RequestSave += (s, a) => LoadUninterruptiblePowerSupplies(system, devices);
      await OpenWindowInDrawerAsync(window, "Редактирование устройства", "F4 - закрыть");
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
    private void LoadBreakdownTesters(IChassisManager chassis, DeviceManagerControl devicesControl)
    {
      devicesControl.ClearDevice(new BreakdownTesterEntity());

      var breakdownTesters = ServiceLocator.GetRequired<BreakdownTesterServices>().GetEntitiesByNumberChassis(chassis.Number);
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
    private void LoadFastMeters(IChassisManager chassis, DeviceManagerControl devicesControl)
    {
      devicesControl.ClearDevice(new FastMeterEntity());
      var fastMeters = FastMeters
        .GetDevicesByNumberChassisAsync(chassis.Number)
        .GetAwaiter()
        .GetResult()
        .Select(ToFastMeterEntity);

      foreach (var device in fastMeters)
      {
        devicesControl.AddDevice(device);
      }
    }

    /// <summary>
    /// Загружает все модули источников питания, привязанные к указанному шасси, и добавляет их в контрол управления устройствами.
    /// </summary>
    /// <param name="chassis">Менеджер шасси.</param>
    /// <param name="devicesControl">Контрол для отображения устройств.</param>
    private void LoadPowerSources(IChassisManager chassis, DeviceManagerControl devicesControl)
    {
      devicesControl.ClearDevice(new PowerSourceModuleEntity());

      var powerSources = PowerSourceModules
        .GetDevicesByNumberChassisAsync(chassis.Number)
        .GetAwaiter()
        .GetResult()
        .Select(ToPowerSourceModuleEntity);

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
    private void LoadRelaySwitchModules(IChassisManager chassis, DeviceManagerControl devicesControl)
    {
      devicesControl.ClearDevice(new RelaySwitchModuleEntity());

      var relaySwitchModules = RelaySwitchModules
        .GetDevicesByNumberChassisAsync(chassis.Number)
        .GetAwaiter()
        .GetResult()
        .Select(ToRelaySwitchModuleEntity);

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
    private void LoadSwitchingDevices(IChassisManager chassis, DeviceManagerControl devicesControl)
    {
      devicesControl.ClearDevice(new SwitchingDeviceEntity());

      var switchingDevices = SwitchingDevices
        .GetDevicesByNumberChassisAsync(chassis.Number)
        .GetAwaiter()
        .GetResult()
        .Select(ToSwitchingDeviceEntity);

      foreach (var device in switchingDevices)
      {
        devicesControl.AddDevice(device);
      }
    }

    /// <summary>
    /// Loads UPS devices bound to selected chassis.
    /// </summary>
    private void LoadUninterruptiblePowerSupplies(IChassisManager chassis, DeviceManagerControl devicesControl)
    {
      devicesControl.ClearDevice(new UninterruptiblePowerSupplyEntity());

      var uninterruptiblePowerSupplies = new UninterruptiblePowerSupplyServices().GetEntitiesByNumberChassis(chassis.Number);

      foreach (var device in uninterruptiblePowerSupplies)
      {
        devicesControl.AddDevice(device);
      }
    }

    /// <summary>
    /// Добавляет систему в список.
    /// </summary>
    /// <param name="data">Данные системы.</param>
    public void AddSystem(IChassisManager data)
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
    /// Перечитывает конфигурацию устройств из БД и обновляет отображение.
    /// </summary>
    public void ReloadConfiguration()
    {
      deviceBorder.Child = null;
      settingsBorder.Child = null;
      ToggleThirdColumn(false);

      chassisManager.Reset();
      var chassisList = ChassisManagers.GetAllAsync().GetAwaiter().GetResult().OrderBy(chassis => chassis.Number).ToList();


      foreach (var chassis in chassisList)
      {
        AddSystem(chassis);
      }

      var racks = Racks
        .GetAllAsync()
        .GetAwaiter()
        .GetResult()
        .Select(ToRackEntity)
        .OrderBy(rack => rack.NumberChassis)
        .ThenBy(rack => rack.Number)
        .ToList();

      foreach (var rack in racks)
      {
        AddRack(rack);
      }

      if (chassisList.Count > 0)
      {
        SelectedChassis(chassisList[0]);
      }
    }

    /// <summary>
    /// Создает новое шасси.
    /// </summary>
    private async void NewSystem()
    {
      ChassisManagerWindow chassisManagerWindow = new ChassisManagerWindow();
      chassisManagerWindow.SetSettings();
      chassisManagerWindow.RequestSave += ChassisManagerSettings_DeviceSaved;

      await OpenWindowInDrawerAsync(chassisManagerWindow, "Добавление системы", "F4 - закрыть", () => Setting_RequestClose(null, EventArgs.Empty));
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

    private async Task OpenWindowInDrawerAsync(BreakDownWindow window, string title, string subtitle, Action? onClose = null)
    {
      window.CloseActionOverride = () => DrawerHostService.Instance.Close();
      var content = window.DetachSettingsControl();
      await DrawerHostService.Instance.OpenContentAsync(content, title, subtitle, onClose, DeviceConfigDrawerPanelWidth);
    }

    private async Task OpenWindowInDrawerAsync(FastMeterWindow window, string title, string subtitle, Action? onClose = null)
    {
      window.CloseActionOverride = () => DrawerHostService.Instance.Close();
      var content = window.DetachSettingsControl();
      await DrawerHostService.Instance.OpenContentAsync(content, title, subtitle, onClose, DeviceConfigDrawerPanelWidth);
    }

    private async Task OpenWindowInDrawerAsync(ModuleVoltageCurrentSourceWindow window, string title, string subtitle, Action? onClose = null)
    {
      window.CloseActionOverride = () => DrawerHostService.Instance.Close();
      var content = window.DetachSettingsControl();
      await DrawerHostService.Instance.OpenContentAsync(content, title, subtitle, onClose, DeviceConfigDrawerPanelWidth);
    }

    private async Task OpenWindowInDrawerAsync(ModuleRelayControlWindow window, string title, string subtitle, Action? onClose = null)
    {
      window.CloseActionOverride = () => DrawerHostService.Instance.Close();
      var content = window.DetachSettingsControl();
      await DrawerHostService.Instance.OpenContentAsync(content, title, subtitle, onClose, DeviceConfigDrawerPanelWidth);
    }

    private async Task OpenWindowInDrawerAsync(DeviceBusCommutationWindow window, string title, string subtitle, Action? onClose = null)
    {
      window.CloseActionOverride = () => DrawerHostService.Instance.Close();
      var content = window.DetachSettingsControl();
      await DrawerHostService.Instance.OpenContentAsync(content, title, subtitle, onClose, DeviceConfigDrawerPanelWidth);
    }

    private async Task OpenWindowInDrawerAsync(ChassisManagerWindow window, string title, string subtitle, Action? onClose = null)
    {
      window.CloseActionOverride = () => DrawerHostService.Instance.Close();
      var content = window.DetachSettingsControl();
      await DrawerHostService.Instance.OpenContentAsync(content, title, subtitle, onClose, DeviceConfigDrawerPanelWidth);
    }

    private async Task OpenWindowInDrawerAsync(UninterruptiblePowerSupplyWindow window, string title, string subtitle, Action? onClose = null)
    {
      window.CloseActionOverride = () => DrawerHostService.Instance.Close();
      var content = window.DetachSettingsControl();
      await DrawerHostService.Instance.OpenContentAsync(content, title, subtitle, onClose, DeviceConfigDrawerPanelWidth);
    }

    private static SwitchingDeviceEntity ToSwitchingDeviceEntity(ISwitchingDevice device)
    {
      return new SwitchingDeviceEntity
      {
        Id = device.Id,
        Name = device.Name,
        Description = device.Description,
        Number = device.Number,
        NumberChassis = device.NumberChassis,
        ConnectionDetails = device.ConnectionDetails,
        DeviceClass = device.DeviceClass,
      };
    }

    private static RelaySwitchModuleEntity ToRelaySwitchModuleEntity(IRelaySwitchModule device)
    {
      return new RelaySwitchModuleEntity
      {
        Id = device.Id,
        Name = device.Name,
        Description = device.Description,
        Number = device.Number,
        NumberChassis = device.NumberChassis,
        PointCount = device.PointCount,
        ConnectionDetails = device.ConnectionDetails,
        DeviceClass = device.DeviceClass,
        SwitchResistance = device.SwitchResistance,
        SwitchCapacitance = device.SwitchCapacitance,
        BusType = device.BusType,
      };
    }

    private static FastMeterEntity ToFastMeterEntity(IFastMeter device)
    {
      return new FastMeterEntity
      {
        Id = device.Id,
        Name = device.Name,
        Description = device.Description,
        Number = device.Number,
        NumberChassis = device.NumberChassis,
        ConnectionDetails = device.ConnectionDetails,
        DeviceClass = device.DeviceClass,
        MaxContinuityResistance = device.MaxContinuityResistance,
        TypeMode = device.TypeMode,
      };
    }

    private static RackEntity ToRackEntity(IRack device)
    {
      return new RackEntity
      {
        Id = device.Id,
        Name = device.Name,
        Description = device.Description,
        Number = device.Number,
        NumberChassis = device.NumberChassis,
        ConnectionDetails = device.ConnectionDetails,
        DeviceClass = device.DeviceClass,
        BusType = device.BusType,
      };
    }

    private static PowerSourceModuleEntity ToPowerSourceModuleEntity(IPowerSourceModule device)
    {
      return new PowerSourceModuleEntity
      {
        Id = device.Id,
        Name = device.Name,
        Description = device.Description,
        Number = device.Number,
        NumberChassis = device.NumberChassis,
        ConnectionDetails = device.ConnectionDetails,
        DeviceClass = device.DeviceClass,
        ResistanceCalibrationJson = device.ResistanceCalibrationJson,
      };
    }
  }
}
