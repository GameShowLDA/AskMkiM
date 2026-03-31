using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.DTO.Devices.ChassisManager;
using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.DTO.Devices.PowerSourceModule;
using Ask.Core.Shared.DTO.Devices.Rack;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Devices.SwitchingDevice;
using Ask.Core.Shared.DTO.Devices.UninterruptiblePowerSupply;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.PowerSourceModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Rack;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.RelaySwitchModule;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.SwitchingDevice;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Support;
using Ask.UI.Infrastructure.UI.Overlay.Drawer.Runtime;
using System.Threading;
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

namespace UI.Controls.Settings.DeviceConfig
{
  /// <summary>
  /// Логика взаимодействия для DeviceConfigControl.xaml.
  /// </summary>
  public partial class DeviceConfigControl : UserControl
  {
    private const double DeviceConfigDrawerPanelWidth = 470d;
    private Task? _initializationTask;
    private CancellationTokenSource? _selectedChassisCancellation;
    private readonly SemaphoreSlim _reloadSemaphore = new(1, 1);
    private bool _isInitialized;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DeviceConfigControl"/>.
    /// </summary>
    public DeviceConfigControl()
    {
      InitializeComponent();
      chassisManager.NewSystem += (s, a) => NewSystem();
      chassisManager.SystemSelected += async (s, a) => await SelectedChassisAsync(a);

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
    private async Task SelectedChassisAsync(IChassisManager system)
    {
      try
      {
        var devices = new DeviceManagerControl();
        devices.SetHeadUnit(system);
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

        ReplaceCancellationTokenSource(ref _selectedChassisCancellation);
        var cancellationToken = _selectedChassisCancellation.Token;

        var breakdownTask = BreakdownTesters.GetDevicesByNumberChassisAsync(system.Number, cancellationToken);
        var fastMetersTask = FastMeters.GetDevicesByNumberChassisAsync(system.Number, cancellationToken);
        var powerSourcesTask = PowerSourceModules.GetDevicesByNumberChassisAsync(system.Number, cancellationToken);
        var relaySwitchModulesTask = RelaySwitchModules.GetDevicesByNumberChassisAsync(system.Number, cancellationToken);
        var switchingDevicesTask = SwitchingDevices.GetDevicesByNumberChassisAsync(system.Number, cancellationToken);
        var uninterruptiblePowerSuppliesTask = UninterruptiblePowerSupplies.GetDevicesByNumberChassisAsync(system.Number, cancellationToken);

        await Task.WhenAll(
          breakdownTask,
          fastMetersTask,
          powerSourcesTask,
          relaySwitchModulesTask,
          switchingDevicesTask,
          uninterruptiblePowerSuppliesTask);

        var breakdownDevices = await breakdownTask;
        var fastMeterDevices = await fastMetersTask;
        var powerSourceDevices = await powerSourcesTask;
        var relaySwitchModuleDevices = await relaySwitchModulesTask;
        var switchingDeviceDevices = await switchingDevicesTask;
        var uninterruptiblePowerSupplyDevices = await uninterruptiblePowerSuppliesTask;

        if (cancellationToken.IsCancellationRequested || deviceBorder.Child != devices)
        {
          return;
        }

        foreach (var device in breakdownDevices.Select(ToBreakdownEntity))
        {
          devices.AddDevice(device);
        }

        foreach (var device in fastMeterDevices.Select(ToFastMeterEntity))
        {
          devices.AddDevice(device);
        }

        foreach (var device in powerSourceDevices.Select(ToPowerSourceModuleEntity))
        {
          devices.AddDevice(device);
        }

        foreach (var device in relaySwitchModuleDevices.Select(ToRelaySwitchModuleEntity))
        {
          devices.AddDevice(device);
        }

        foreach (var device in switchingDeviceDevices.Select(ToSwitchingDeviceEntity))
        {
          devices.AddDevice(device);
        }

        foreach (var device in uninterruptiblePowerSupplyDevices.Select(ToUninterruptiblePowerSupplyEntity))
        {
          devices.AddDevice(device);
        }
      }
      catch (OperationCanceledException)
      {
      }
    }

    /// <summary>
    /// Отображает окно быстрого измерителя.
    /// </summary>
    private async void Devices_FastMeterEvent(object? sender, IHeadUnit e, IChassisManager system, DeviceManagerControl devices)
    {
      FastMeterWindow fastMeterWindow = new FastMeterWindow();
      fastMeterWindow.SetSettings(sender, e);
      fastMeterWindow.RequestSave += async (s, a) => await SelectedChassisAsync(system);
      await OpenWindowInDrawerAsync(fastMeterWindow, "Добавление устройства", "F4 - закрыть");
    }

    /// <summary>
    /// Отображает окно источника напряжения.
    /// </summary>
    private async void Devices_PowerModuleEvent(object? sender, IHeadUnit e, IChassisManager system, DeviceManagerControl devices)
    {
      ModuleVoltageCurrentSourceWindow fastMeterWindow = new ModuleVoltageCurrentSourceWindow();
      fastMeterWindow.SetSettings(sender, e);
      fastMeterWindow.RequestSave += async (s, a) => await SelectedChassisAsync(system);
      await OpenWindowInDrawerAsync(fastMeterWindow, "Добавление устройства", "F4 - закрыть");
    }

    /// <summary>
    /// Отображает окно источника напряжения.
    /// </summary>
    private async void Devices_ModuleRalayEvent(object? sender, IHeadUnit e, IChassisManager system, DeviceManagerControl devices)
    {
      ModuleRelayControlWindow fastMeterWindow = new ModuleRelayControlWindow();
      fastMeterWindow.SetSettings(sender, e);
      fastMeterWindow.RequestSave += async (s, a) => await SelectedChassisAsync(system);
      await OpenWindowInDrawerAsync(fastMeterWindow, "Добавление устройства", "F4 - закрыть");
    }

    /// <summary>
    /// Отображает окно коммутации шин.
    /// </summary>
    private async void Devices_DeviceBusCommutationSelected(object? sender, IHeadUnit e, IChassisManager system, DeviceManagerControl devices)
    {
      DeviceBusCommutationWindow deviceSettingsWindow = new DeviceBusCommutationWindow();
      deviceSettingsWindow.SetSettings(sender, e);
      deviceSettingsWindow.RequestSave += async (s, a) => await SelectedChassisAsync(system);
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
      fastMeterWindow.RequestSave += async (s, a) => await SelectedChassisAsync(system);
      await OpenWindowInDrawerAsync(fastMeterWindow, "Добавление устройства", "F4 - закрыть");
    }

    private async void Devices_EditBreakdownEvent(IChassisManager system, DeviceManagerControl devices, BreakdownTesterDto entity)
    {
      BreakDownWindow window = new BreakDownWindow();
      window.SetSettings(this, system, entity);
      window.RequestSave += async (s, a) => await SelectedChassisAsync(system);
      await OpenWindowInDrawerAsync(window, "Редактирование устройства", "F4 - закрыть");
    }

    private async void Devices_EditSwitchingEvent(IChassisManager system, DeviceManagerControl devices, SwitchingDeviceDto entity)
    {
      DeviceBusCommutationWindow window = new DeviceBusCommutationWindow();
      window.SetSettings(this, system, entity);
      window.RequestSave += async (s, a) => await SelectedChassisAsync(system);
      await OpenWindowInDrawerAsync(window, "Редактирование устройства", "F4 - закрыть");
    }

    private async void Devices_EditPowerModuleEvent(IChassisManager system, DeviceManagerControl devices, PowerSourceModuleDto entity)
    {
      ModuleVoltageCurrentSourceWindow window = new ModuleVoltageCurrentSourceWindow();
      window.SetSettings(this, system, entity);
      window.RequestSave += async (s, a) => await SelectedChassisAsync(system);
      await OpenWindowInDrawerAsync(window, "Редактирование устройства", "F4 - закрыть");
    }

    private async void Devices_EditRelayEvent(IChassisManager system, DeviceManagerControl devices, RelaySwitchModuleDto entity)
    {
      ModuleRelayControlWindow window = new ModuleRelayControlWindow();
      window.SetSettings(this, system, entity);
      window.RequestSave += async (s, a) => await SelectedChassisAsync(system);
      await OpenWindowInDrawerAsync(window, "Редактирование устройства", "F4 - закрыть");
    }

    private async void Devices_EditFastMeterEvent(IChassisManager system, DeviceManagerControl devices, FastMeterDto entity)
    {
      FastMeterWindow window = new FastMeterWindow();
      window.SetSettings(this, system, entity);
      window.RequestSave += async (s, a) => await SelectedChassisAsync(system);
      await OpenWindowInDrawerAsync(window, "Редактирование устройства", "F4 - закрыть");
    }

    private async void Devices_UninterruptiblePowerSupplyEvent(object? sender, IHeadUnit e, IChassisManager system, DeviceManagerControl devices)
    {
      UninterruptiblePowerSupplyWindow window = new UninterruptiblePowerSupplyWindow();
      window.SetSettings(sender, e);
      window.RequestSave += async (s, a) => await SelectedChassisAsync(system);
      await OpenWindowInDrawerAsync(window, "Добавление устройства", "F4 - закрыть");
    }

    private async void Devices_EditUninterruptiblePowerSupplyEvent(IChassisManager system, DeviceManagerControl devices, UninterruptiblePowerSupplyDto entity)
    {
      UninterruptiblePowerSupplyWindow window = new UninterruptiblePowerSupplyWindow();
      window.SetSettings(this, system, entity);
      window.RequestSave += async (s, a) => await SelectedChassisAsync(system);
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
    /// <summary>
    /// Добавляет систему в список.
    /// </summary>
    /// <param name="data">Данные системы.</param>
    public void AddSystem(IChassisManager data)
    {
      chassisManager.AddSystem(data.Convert());
    }

    /// <summary>
    /// Добавляет стойку в список.
    /// </summary>
    /// <param name="data">Данные стойки.</param>
    public void AddRack(RackDto data)
    {
      chassisManager.AddRack(data);
    }

    /// <summary>
    /// Перечитывает конфигурацию устройств из БД и обновляет отображение.
    /// </summary>
    public async Task EnsureInitializedAsync()
    {
      if (_isInitialized)
      {
        return;
      }

      if (_initializationTask != null)
      {
        await _initializationTask;
        return;
      }

      _initializationTask = ReloadConfigurationAsync();
      await _initializationTask;
    }

    public async Task ReloadConfigurationAsync(CancellationToken cancellationToken = default)
    {
      await _reloadSemaphore.WaitAsync(cancellationToken);

      try
      {
        deviceBorder.Child = null;
        settingsBorder.Child = null;
        ToggleThirdColumn(false);

        chassisManager.Reset();

        var chassisTask = ChassisManagers.GetAllAsync(cancellationToken);
        var racksTask = Racks.GetAllAsync(cancellationToken);
        await Task.WhenAll(chassisTask, racksTask);

        var chassisData = await chassisTask;
        var racksData = await racksTask;

        var chassisList = chassisData
          .OrderBy(chassis => chassis.Number)
          .ToList();

        foreach (var chassis in chassisList)
        {
          AddSystem(chassis);
        }

        var racks = racksData
          .Select(ToRackEntity)
          .OrderBy(rack => rack.NumberChassis)
          .ThenBy(rack => rack.Number)
          .ToList();

        foreach (var rack in racks)
        {
          AddRack(rack);
        }

        _isInitialized = true;

        if (chassisList.Count > 0)
        {
          await SelectedChassisAsync(chassisList[0]);
        }
      }
      catch (OperationCanceledException)
      {
      }
      finally
      {
        _reloadSemaphore.Release();
      }
    }

    public void ReloadConfiguration()
    {
      _ = ReloadConfigurationAsync();
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
    private void ChassisManagerSettings_DeviceSaved(object sender, ChassisManagerDto device)
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

    private static SwitchingDeviceDto ToSwitchingDeviceEntity(ISwitchingDevice device) => device.Convert();

    private static RelaySwitchModuleDto ToRelaySwitchModuleEntity(IRelaySwitchModule device) => device.Convert();

    private static FastMeterDto ToFastMeterEntity(IFastMeter device) => device.Convert();

    private static RackDto ToRackEntity(IRack device) => device.Convert();
    private static PowerSourceModuleDto ToPowerSourceModuleEntity(IPowerSourceModule device) => device.Convert();

    private static UninterruptiblePowerSupplyDto ToUninterruptiblePowerSupplyEntity(IUninterruptiblePowerSupply device) => device.Convert();
    private static BreakdownTesterDto ToBreakdownEntity(IBreakdownTester device) => device.Convert();

    private static void ReplaceCancellationTokenSource(ref CancellationTokenSource? cancellationTokenSource)
    {
      cancellationTokenSource?.Cancel();
      cancellationTokenSource?.Dispose();
      cancellationTokenSource = new CancellationTokenSource();
    }
  }
}
