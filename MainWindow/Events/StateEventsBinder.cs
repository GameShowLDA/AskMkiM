using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.Core.Shared.Metadata.Enums.RoleEnums;
using Ask.DataBase.Engine.Static.Devices;
using Ask.UI.Infrastructure.UI.Overlay.Drawer.Runtime;
using ConsoleUI.ConsoleCommanding.Commands;
using ConsoleUI.ConsoleCommanding.Services;
using ConsoleUI.ConsoleLogic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace MainWindowProgram.Events
{
  public class StateEventsBinder
  {
    private HotkeyListenerService _hotkey;
    private readonly MainWindow _mainWindow;
    private static bool isLocked = false;

    public StateEventsBinder(MainWindow mainWindow)
    {
      _mainWindow = mainWindow;
    }

    public void Bind()
    {
      EventAggregator.Subscribe<SystemStateEvents.LockedChanged>(e => OnLockedChanged(e.IsLocked));
      EventAggregator.Subscribe<SystemStateEvents.AdminRightsChanged>(e => OnAdminRightsChanged(e.IsAdmin));
      EventAggregator.Subscribe<SystemStateEvents.ControlProgramActiveChanged>(e => OnControlProgramActiveRightsChanged(e.IsControlProgramActive));
      EventAggregator.Subscribe<SystemStateEvents.ConsoleAccessChanged>(e => OnConsoleAccessChanged(e.IsEnabled));
      EventAggregator.Subscribe<SystemStateEvents.TestsMenuVisibilityChanged>(e => OnTestsMenuVisibilityChanged(e.IsVisible));
      EventAggregator.Subscribe<SystemStateEvents.PowerChanged>(OnPowerChanged);

      ExecutionConfig.IdleModeChange += OnIdleModeChange;

      AdminCommand.AdminModeChanged += AdminModeChanged;
      AdminCommand.PauseInStopChanged += AdminCommand_PauseInStopChanged;
      AdminCommand.PowerChanged += AdminCommand_PowerChanged;
      AdminCommand.UpsPowerChanged += AdminCommand_UpsPowerChanged;

      _mainWindow.PreviewKeyDown += OnKeyDown;

      bool idleMode = ExecutionConfig.GetIsIdleModeEnabled();
      EventAggregator.Subscribe<ThemeEvent.Change>(OnThemeChanged);

      OnIdleModeChange(null, idleMode);
      OnConsoleAccessChanged(RoleAuthorizationConfig.CurrentRole == RoleType.Root);
      OnTestsMenuVisibilityChanged(RoleAuthorizationConfig.CurrentRole != RoleType.Developer);
    }

    private void AdminCommand_PowerChanged(object? sender, bool e)
    {
      SystemStateManager.SetIsActivePower(e);
    }

    private void AdminCommand_UpsPowerChanged(object? sender, bool e)
    {
      Application.Current.Dispatcher.BeginInvoke(async () =>
      {
        try
        {
          IUninterruptiblePowerSupply? ups = GetConfiguredUps();
          if (ups == null)
          {
            MessageEventAdapter.RaiseErrorMessage("Бесперебойник не найден в конфигурации.", true);
            return;
          }

          if (e)
          {
            await ups.PowerManager.StartPowerAsync();
          }
          else
          {
            await ups.PowerManager.StopPowerAsync();
          }
        }
        catch (Exception ex)
        {
          MessageEventAdapter.RaiseErrorMessage(ex.Message, true);
        }
      });
    }

    private static IUninterruptiblePowerSupply? GetConfiguredUps()
    {
      int? chassisNumber = ChassisManagers.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault()?.Number;

      IEnumerable<IUninterruptiblePowerSupply> devices = UninterruptiblePowerSupplies.GetAllAsync().GetAwaiter().GetResult();

      if (chassisNumber.HasValue)
      {
        return devices.FirstOrDefault(device => device.NumberChassis == chassisNumber.Value)
          ?? devices.FirstOrDefault();
      }

      return devices.FirstOrDefault();
    }

    private void AdminCommand_PauseInStopChanged(object? sender, bool e)
    {
      ExecutionConfig.SetStopOnError(e);
    }

    private void OnThemeChanged(ThemeEvent.Change e)
    {
      Application.Current.Dispatcher.BeginInvoke(ApplyMainPanelBackground);
    }

    private void AdminModeChanged(object? sender, bool e)
    {
      AdminConfig.SetAdminRights(e && RoleAuthorizationConfig.CurrentRole == RoleType.Root);
    }

    private void OnIdleModeChange(object? sender, bool e)
    {
      Application.Current.Dispatcher.BeginInvoke(() =>
      {
        _mainWindow.PowerButton.Visibility = e ? Visibility.Collapsed : Visibility.Visible;
        ApplyExecutionIndicatorColor(e);
        ApplyMainPanelBackground();
      });
    }

    private void ApplyExecutionIndicatorColor(bool isIdleMode)
    {
      string brushKey = isIdleMode
        ? "NotificationSuccessIconBrush"
        : "NotificationErrorIconBrush";

      _mainWindow.UploadErrorIndicator.SetResourceReference(
        System.Windows.Controls.Control.ForegroundProperty,
        brushKey);
    }

    private void OnPowerChanged(SystemStateEvents.PowerChanged _)
    {
      Application.Current.Dispatcher.BeginInvoke(ApplyMainPanelBackground);
    }

    private void ApplyMainPanelBackground()
    {
      Brush topBrush = (Brush)Application.Current.FindResource("BackgroundBrushes");
      string bottomBrushKey = SystemStateManager.GetIsActivePower()
        ? "SystemPowerWarningPanelBrush"
        : "BackgroundBrushes";

      Brush bottomBrush = (Brush)Application.Current.FindResource(bottomBrushKey);
      _mainWindow.BottomPanel.Background = bottomBrush;
      _mainWindow.TopPanel.Background = topBrush;
    }

    private void OnLockedChanged(bool newValue)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        if (newValue)
        {
          ApplyExecutionIndicatorColor(ExecutionConfig.GetIsIdleModeEnabled());
          _mainWindow.TopPanel.Visibility = Visibility.Collapsed;
          _mainWindow.UploadErrorIndicator.Visibility = Visibility.Visible;
          if (!ExecutionConfig.GetIsIdleModeEnabled())
          {
            _mainWindow.PowerButton.Visibility = Visibility.Collapsed;
          }

          isLocked = true;
        }
        else
        {
          _mainWindow.TopPanel.Visibility = Visibility.Visible;
          _mainWindow.UploadErrorIndicator.Visibility = Visibility.Collapsed;
          if (!ExecutionConfig.GetIsIdleModeEnabled())
          {
            _mainWindow.PowerButton.Visibility = Visibility.Visible;
          }

          isLocked = false;
        }
      });
    }

    private void OnAdminRightsChanged(bool isAdmin)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        _mainWindow.Admin.Visibility = isAdmin
          ? Visibility.Visible
          : Visibility.Collapsed;
      });
    }

    private void OnConsoleAccessChanged(bool isEnabled)
    {
      ConsoleVisibilityController.SetEnabled(isEnabled);

      Application.Current.Dispatcher.Invoke(() =>
      {
        _mainWindow.TerminalButton.Visibility = isEnabled
          ? Visibility.Visible
          : Visibility.Collapsed;
      });
    }

    private void OnTestsMenuVisibilityChanged(bool isVisible)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        _mainWindow.TestMenu.Visibility = isVisible
          ? Visibility.Visible
          : Visibility.Collapsed;
      });
    }

    private void OnControlProgramActiveRightsChanged(bool isControlProgramActive)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        _mainWindow.Translation.Visibility = Visibility.Visible;
        _mainWindow.Translation.IsEnabled = true;
        _mainWindow.Build.Visibility = Visibility.Visible;
        _mainWindow.Build.IsEnabled = true;
        _mainWindow.Run.Visibility = Visibility.Visible;
        _mainWindow.Run.IsEnabled = true;
        _mainWindow.RunStepByStepMode.Visibility = Visibility.Visible;
        _mainWindow.RunStepByStepMode.IsEnabled = true;
      });
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
      if (DrawerHostService.Instance.ShouldBlockGlobalInput)
      {
        return;
      }

      if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.Oem3)
      {
        ConsoleVisibilityController.ToggleConsole();
        e.Handled = true;
      }
    }
  }
}
