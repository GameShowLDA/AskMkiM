using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.DTO.Devices.Breakdown;
using Ask.Core.Shared.DTO.Devices.FastMeter;
using Ask.Core.Shared.DTO.Devices.PowerSourceModule;
using Ask.Core.Shared.DTO.Devices.RelaySwitchModule;
using Ask.Core.Shared.DTO.Devices.SwitchingDevice;
using Ask.Core.Shared.DTO.Devices.UninterruptiblePowerSupply;
using Ask.Core.Shared.Entity.Devices;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using System.Windows;
using System.Windows.Controls;
using static Ask.Core.Services.EventCore.Events.SystemStateEvents;

namespace UI.Controls.Settings.DeviceConfig.DeviceManager
{
  /// <summary>
  /// Interaction logic for DeviceManagerControl.xaml.
  /// </summary>
  public partial class DeviceManagerControl : UserControl
  {
    /// <summary>
    /// Event raised when breakdown tester creation is requested.
    /// </summary>
    public event EventHandler<IHeadUnit> AddBreakdownEvent;

    /// <summary>
    /// Event raised when switching device creation is requested.
    /// </summary>
    public event EventHandler<IHeadUnit> DeviceBusCommutationSelected;

    /// <summary>
    /// Event raised when fast meter creation is requested.
    /// </summary>
    public event EventHandler<IHeadUnit> FastMeterEvent;

    /// <summary>
    /// Event raised when power source module creation is requested.
    /// </summary>
    public event EventHandler<IHeadUnit> PowerModuleEvent;

    /// <summary>
    /// Event raised when relay module creation is requested.
    /// </summary>
    public event EventHandler<IHeadUnit> ModuleRelayEvent;

    /// <summary>
    /// Event raised when UPS creation is requested.
    /// </summary>
    public event EventHandler<IHeadUnit> UninterruptiblePowerSupplyEvent;

    public event EventHandler<BreakdownTesterDto> EditBreakdownEvent;
    public event EventHandler<FastMeterDto> EditFastMeterEvent;
    public event EventHandler<PowerSourceModuleDto> EditPowerModuleEvent;
    public event EventHandler<RelaySwitchModuleDto> EditModuleRelayEvent;
    public event EventHandler<SwitchingDeviceDto> EditDeviceBusCommutationEvent;
    public event EventHandler<UninterruptiblePowerSupplyDto> EditUninterruptiblePowerSupplyEvent;

    /// <summary>
    /// Event raised when exit is requested.
    /// </summary>
    public event EventHandler ExitEvent;

    private IHeadUnit _headUnit;
    private readonly Action<AdminRightsChanged> _adminRightsChangedHandler;
    private bool _adminRightsSubscribed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceManagerControl"/> class.
    /// </summary>
    public DeviceManagerControl()
    {
      InitializeComponent();

      FastMeterControl.IsSingleDeviceOnly = true;
      PrecisionMeterControl.IsSingleDeviceOnly = true;
      BreakdownTesterControl.IsSingleDeviceOnly = true;
      SwitchingDeviceControl.IsSingleDeviceOnly = true;
      UninterruptiblePowerSupplyControl.IsSingleDeviceOnly = true;

      BreakdownTesterControl.PlusEvent += (s, a) => AddBreakdownEvent?.Invoke(this, _headUnit);
      SwitchingDeviceControl.PlusEvent += (s, a) => DeviceBusCommutationSelected?.Invoke(this, _headUnit);
      FastMeterControl.PlusEvent += (s, a) => FastMeterEvent?.Invoke(this, _headUnit);
      PowerSourceModuleControl.PlusEvent += (s, a) => PowerModuleEvent?.Invoke(this, _headUnit);
      RelaySwitchModuleControl.PlusEvent += (s, a) => ModuleRelayEvent?.Invoke(this, _headUnit);
      UninterruptiblePowerSupplyControl.PlusEvent += (s, a) => UninterruptiblePowerSupplyEvent?.Invoke(this, _headUnit);

      BreakdownTesterControl.EditEvent += (s, device) =>
      {
        if (device is BreakdownTesterDto entity)
        {
          EditBreakdownEvent?.Invoke(this, entity);
        }
      };

      FastMeterControl.EditEvent += (s, device) =>
      {
        if (device is FastMeterDto entity)
        {
          EditFastMeterEvent?.Invoke(this, entity);
        }
      };

      PowerSourceModuleControl.EditEvent += (s, device) =>
      {
        if (device is PowerSourceModuleDto entity)
        {
          EditPowerModuleEvent?.Invoke(this, entity);
        }
      };

      RelaySwitchModuleControl.EditEvent += (s, device) =>
      {
        if (device is RelaySwitchModuleDto entity)
        {
          EditModuleRelayEvent?.Invoke(this, entity);
        }
      };

      SwitchingDeviceControl.EditEvent += (s, device) =>
      {
        if (device is SwitchingDeviceDto entity)
        {
          EditDeviceBusCommutationEvent?.Invoke(this, entity);
        }
      };

      UninterruptiblePowerSupplyControl.EditEvent += (s, device) =>
      {
        if (device is UninterruptiblePowerSupplyDto entity)
        {
          EditUninterruptiblePowerSupplyEvent?.Invoke(this, entity);
        }
      };

      _adminRightsChangedHandler = OnAdminRightsChanged;

      Loaded += DeviceManagerControl_Loaded;
      Unloaded += DeviceManagerControl_Unloaded;
    }

    /// <summary>
    /// Sets selected chassis manager instance.
    /// </summary>
    public void SetHeadUnit<T>(T headUnit) where T : class, IHeadUnit
    {
      _headUnit = headUnit;
    }

    /// <summary>
    /// Handles exit action.
    /// </summary>
    private void Exit_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      ExitEvent?.Invoke(this, EventArgs.Empty);
    }

    private void DeviceManagerControl_Loaded(object sender, RoutedEventArgs e)
    {
      if (!_adminRightsSubscribed)
      {
        EventAggregator.Subscribe(_adminRightsChangedHandler);
        _adminRightsSubscribed = true;
      }

      UpdateUpsVisibility(AdminConfig.GetAdminRights());
    }

    private void DeviceManagerControl_Unloaded(object sender, RoutedEventArgs e)
    {
      if (_adminRightsSubscribed)
      {
        EventAggregator.Unsubscribe(_adminRightsChangedHandler);
        _adminRightsSubscribed = false;
      }
    }

    private void OnAdminRightsChanged(AdminRightsChanged e)
    {
      Dispatcher.Invoke(() => UpdateUpsVisibility(e.IsAdmin));
    }

    private void UpdateUpsVisibility(bool isAdmin)
    {
      UninterruptiblePowerSupplyControl.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
    }
  }
}
