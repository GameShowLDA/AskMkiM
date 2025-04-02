using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using AppConfiguration.Base;
using AppConfiguration.Execution;
using AppConfiguration.SystemState;
using ConsoleUtilities;
using MainWindowProgram.Services;
using UI.Components;
using Utilities.USB;

namespace MainWindowProgram.Events
{
  /// <summary>
  /// Класс <c>StateEventsBinder</c> подписывает обработчики на события изменения состояния системы
  /// и обновляет пользовательский интерфейс <see cref="MainWindow"/> в ответ на эти события.
  /// </summary>
  public class StateEventsBinder
  {
    /// <summary>
    /// Сервис отслеживания подключения и отключения USB-устройств.
    /// Используется для реагирования на смену прав администратора.
    /// </summary>
    private readonly UsbServices _usbMonitorService;

    /// <summary>
    /// Ссылка на главное окно приложения, интерфейс которого необходимо обновлять.
    /// </summary>
    private readonly MainWindow _mainWindow;

    /// <summary>
    /// Флаг, указывающий, находится ли интерфейс в заблокированном состоянии.
    /// Используется для предотвращения повторного применения изменений.
    /// </summary>
    private static bool isLocked = false;

    private readonly ConsoleManager _consoleManager;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="StateEventsBinder"/>.
    /// </summary>
    /// <param name="mainWindow">Ссылка на главное окно приложения.</param>
    /// <param name="usbMonitorService">Сервис мониторинга USB-подключений.</param>
    public StateEventsBinder(MainWindow mainWindow, UsbServices usbMonitorService, ConsoleManager consoleManager)
    {
      _usbMonitorService = usbMonitorService;
      _mainWindow = mainWindow;
      _consoleManager = consoleManager;
    }

    /// <summary>
    /// Подписывает обработчики событий на события изменения состояния приложения.
    /// </summary>
    public void Bind()
    {
      EventAggregator.LockedChanged += OnLockedChanged;
      EventAggregator.AdminRightsChanged += OnAdminRightsChanged;
      ExecutionConfig.IdleModeChange += OnIdleModeChange;
      _consoleManager.AdminModeChanged += AdminModeChanged;
      _usbMonitorService.UsbMonitorService.AdminRightsChanged += OnAdminRightsChangedHandler;
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
        if (e)
        {
          _mainWindow.BottomPanel.Background = (Brush)Application.Current.FindResource("GreenColorSolidColorBrush");
          _mainWindow.TopPanel.Background = (Brush)Application.Current.FindResource("GreenColorSolidColorBrush");
          _mainWindow.PowerButton.Visibility = Visibility.Collapsed;
        }
        else
        {
          _mainWindow.BottomPanel.Background = (Brush)Application.Current.FindResource("SecondarySolidColorBrush");
          _mainWindow.TopPanel.Background = (Brush)Application.Current.FindResource("SecondarySolidColorBrush");
          _mainWindow.PowerButton.Visibility = Visibility.Visible;
        }
      });
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
          isLocked = true;
        }
        else
        {
          _mainWindow.TopPanel.Visibility = Visibility.Visible;
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
    /// Обрабатывает событие от <see cref="USBMonitorService"/> об изменении прав администратора
    /// и передаёт это состояние в <see cref="SystemStateManager"/>.
    /// </summary>
    /// <param name="sender">Источник события (обычно <see cref="USBMonitorService"/>).</param>
    /// <param name="newRights">Новое состояние прав администратора.</param>
    private void OnAdminRightsChangedHandler(object sender, bool newRights)
    {
      SystemStateManager.SetAdminRights(newRights).ConfigureAwait(true);
    }

    /// <summary>
    /// Обрабатывает нажатия клавиш в главном окне.
    /// Если нажаты Ctrl + Oem3, переключает видимость консоли.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы события нажатия клавиши.</param>
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
      if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.Oem3)
      {
        _consoleManager.ToggleConsole();
        e.Handled = true;
      }
    }
  }
}
