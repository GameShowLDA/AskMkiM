using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.Core.Shared.Metadata.View;
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
  /// <summary>
  /// Класс <c>StateEventsBinder</c> подписывает обработчики на события изменения состояния системы
  /// и обновляет пользовательский интерфейс <see cref="MainWindow"/> в ответ на эти события.
  /// </summary>
  public class StateEventsBinder
  {
    private HotkeyListenerService _hotkey;

    /// <summary>
    /// Сервис отслеживания подключения и отключения USB-устройств.
    /// Используется для реагирования на смену прав администратора.
    /// </summary>
    private readonly IUsbMonitorView _usbMonitorService;

    /// <summary>
    /// Ссылка на главное окно приложения, интерфейс которого необходимо обновлять.
    /// </summary>
    private readonly MainWindow _mainWindow;

    /// <summary>
    /// Флаг, указывающий, находится ли интерфейс в заблокированном состоянии.
    /// Используется для предотвращения повторного применения изменений.
    /// </summary>
    private static bool isLocked = false;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="StateEventsBinder"/>.
    /// </summary>
    /// <param name="mainWindow">Ссылка на главное окно приложения.</param>
    /// <param name="usbMonitorService">Сервис мониторинга USB-подключений.</param>
    public StateEventsBinder(MainWindow mainWindow, IUsbMonitorView usbMonitorService)
    {
      _usbMonitorService = usbMonitorService;
      _mainWindow = mainWindow;
    }

    /// <summary>
    /// Подписывает обработчики событий на события изменения состояния приложения.
    /// </summary>
    public void Bind()
    {
      EventAggregator.Subscribe<SystemStateEvents.LockedChanged>(e => OnLockedChanged(e.IsLocked));
      EventAggregator.Subscribe<SystemStateEvents.AdminRightsChanged>(e => OnAdminRightsChanged(e.IsAdmin));
      EventAggregator.Subscribe<SystemStateEvents.ControlProgramActiveChanged>(e => OnControlProgramActiveRightsChanged(e.IsControlProgramActive));
      EventAggregator.Subscribe<SystemStateEvents.PowerChanged>(OnPowerChanged);

      ExecutionConfig.IdleModeChange += OnIdleModeChange;

      AdminCommand.AdminModeChanged += AdminModeChanged;
      DebugCommand.DebugModeChanged += DebugModeChanged;
      AdminCommand.PauseInStopChanged += AdminCommand_PauseInStopChanged;
      AdminCommand.PowerChanged += AdminCommand_PowerChanged;
      AdminCommand.UpsPowerChanged += AdminCommand_UpsPowerChanged;

      _usbMonitorService.AdminRightsChanged += OnAdminRightsChangedHandler;
      _mainWindow.PreviewKeyDown += OnKeyDown;

      bool idleMode = ExecutionConfig.GetIsIdleModeEnabled();
      EventAggregator.Subscribe<ThemeEvent.Change>(OnThemeChanged);

      OnIdleModeChange(null, idleMode);
    }

    private void DebugModeChanged(object? sender, bool e)
    {
      AdminConfig.SetDebugRights(e).ConfigureAwait(true);
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

    /// <summary>
    /// Обработчик события смены темы. Вызывается, когда тема меняется глобально.
    /// </summary>
    private void OnThemeChanged(ThemeEvent.Change e)
    {
      Application.Current.Dispatcher.BeginInvoke(ApplyMainPanelBackground);
    }

    /// <summary>
    /// Обработчик события изменения режима администратора от менеджера консоли.
    /// Останавливает или запускает мониторинг USB в зависимости от новых прав.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Новое значение режима администратора.</param>
    private void AdminModeChanged(object? sender, bool e)
    {
      if (e)
      {
        _usbMonitorService.StopUsbMonitoring();
        OnAdminRightsChangedHandler(null, true);
      }
      else
      {
        OnAdminRightsChangedHandler(null, false);
        _usbMonitorService.SetUsbMonitoring(false);
      }
    }

    /// <summary>
    /// Обрабатывает изменение режима ожидания (Idle Mode) и обновляет интерфейс в зависимости от нового состояния.
    /// </summary>
    /// <param name="sender">Источник события (не используется).</param>
    /// <param name="e">Новое значение режима ожидания: <c>true</c> — режим активен, <c>false</c> — режим отключён.</param>
    private void OnIdleModeChange(object? sender, bool e)
    {
      Application.Current.Dispatcher.BeginInvoke(() =>
      {
        _mainWindow.PowerButton.Visibility = e ? Visibility.Collapsed : Visibility.Visible;
        ApplyMainPanelBackground();
      });
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

    /// <summary>
    /// Обрабатывает событие изменения состояния блокировки интерфейса.
    /// Скрывает или отображает верхнюю панель окна в зависимости от нового значения.
    /// </summary>
    /// <param name="newValue">Новое состояние блокировки: <c>true</c> — интерфейс заблокирован; <c>false</c> — разблокирован.</param>
    private void OnLockedChanged(bool newValue)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        if (newValue)
        {
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

    /// <summary>
    /// Обрабатывает изменение прав администратора и обновляет видимость панели администратора.
    /// </summary>
    /// <param name="isAdmin">Флаг наличия прав администратора: <c>true</c> — есть; <c>false</c> — нет.</param>
    private void OnAdminRightsChanged(bool isAdmin)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        if (isAdmin)
        {
          _mainWindow.Admin.Visibility = Visibility.Visible;
        }
        else
        {
          _mainWindow.Admin.Visibility = Visibility.Collapsed;
        }
      });
    }

    /// <summary>
    /// Обрабатывает какой файл открыт.
    /// </summary>
    /// <param name="isControlProgramActive">Флаг необходимости отображения кнопки выполнения: <c>true</c> — отображать; <c>false</c> — не отображать.</param>
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

    /// <summary>
    /// Обрабатывает событие от <see cref="USBMonitorService"/> об изменении прав администратора
    /// и передаёт это состояние в <see cref="SystemStateManager"/>.
    /// </summary>
    /// <param name="sender">Источник события (обычно <see cref="USBMonitorService"/>).</param>
    /// <param name="newRights">Новое состояние прав администратора.</param>
    private void OnAdminRightsChangedHandler(object sender, bool newRights)
    {
      AdminConfig.SetAdminRights(newRights);
    }

    /// <summary>
    /// Обрабатывает нажатия клавиш в главном окне.
    /// Если нажаты Ctrl + Oem3, переключает видимость консоли.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события нажатия клавиши.</param>
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
