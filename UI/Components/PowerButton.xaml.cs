using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.UninterruptiblePowerSupply;
using Ask.DataBase.Engine.Static.Devices;
using Ask.Device.Runtime.Base.Device;
using Ask.Device.Runtime.Ethernet.Udp.Broadcast;
using Ask.UI.Features.Notifications.Models;
using Ask.UI.Infrastructure.UI.Overlay.Notifications.Runtime;
using Message;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UI.Components
{
  /// <summary>
  /// PowerButton - элемент управления для включения/отключения питания системы.
  /// Управляет подключением к системе, изменением состояния кнопки и выводом сообщений
  /// об ошибках подключения. Асинхронно обрабатывает операции включения/отключения.
  /// </summary>
  public partial class PowerButton : UserControl
  {
    #region Поля.

    /// <summary>
    /// Модель конфигурации устройства, к которому осуществляется подключение.
    /// </summary>
    private IChassisManager model;

    /// <summary>
    /// Флаг, указывающий, активно ли подключение системы (true - подключено, false - отключено).
    /// </summary>
    private static bool active = false;

    /// <summary>
    /// Статический флаг, показывающий, выполняется ли в данный момент задача подключения/отключения.
    /// </summary>
    private static bool taskInProgress = false;

    /// <summary>
    /// Статический флаг, указывающий на наличие ошибки подключения.
    /// </summary>
    private static bool hasError = false;

    /// <summary>
    /// Токен для отмены асинхронных задач при необходимости.
    /// </summary>
    private CancellationTokenSource cancellationToken;
    #endregion

    /// <summary>
    /// Инициализация PowerButton, подписка на события и настройки поведения кнопки.
    /// </summary>
    public PowerButton()
    {
      InitializeComponent();
      Loaded += OnLoaded;
    }

    /// <summary>
    /// Обработка события загрузки, инициализация модели устройства и обновление видимости подсказок.
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      SetButtonToolTip("Подключить систему");
      SetDisconnectedState("Подключить систему");
      EventAggregator.Subscribe<SystemStateEvents.PowerChanged>(OnPowerChanged);
    }

    /// <summary>
    /// Обработка изменения состояния питания для синхронизации с внешними изменениями.
    /// </summary>
    private void OnPowerChanged(SystemStateEvents.PowerChanged e)
    {
      bool isPowered = e.IsPowered;

      if (isPowered)
      {
        Console.WriteLine("⚡ Питание включено");
      }
      else
      {
        Console.WriteLine("❌ Питание отключено");
      }
    }

    /// <summary>
    /// Обработчик события клика по кнопке питания.
    /// </summary>
    public async void OnPowerButtonClick(object sender, MouseButtonEventArgs e)
    {
      await PowerButtonClick();
    }

    /// <summary>
    /// Обработчик нажатия кнопки питания.
    /// </summary>
    private async void OnPowerActionButtonClick(object sender, RoutedEventArgs e)
    {
      await PowerButtonClick();
    }

    /// <summary>
    /// Переключает состояние питания через UI-кнопку.
    /// </summary>
    public async Task PowerButtonClick()
    {
      if (!TryResolveChassisManager() || !ValidatePowerActionAvailability())
      {
        return;
      }

      if (taskInProgress)
      {
        SetDisconnectedState("Подключить систему");
        cancellationToken.Cancel();
        return;
      }

      cancellationToken = new CancellationTokenSource();
      taskInProgress = true;
      hasError = false;

      SetLoadingState("Загрузка...", (Color)FindResource("ActiveColor"));

      try
      {
        if (!active)
        {
          await StartPowerSequenceAsync();
        }
        else
        {
          await StopPowerSequenceAsync();
        }
      }
      finally
      {
        taskInProgress = false;
        SystemStateManager.SetIsActivePower(active);
      }
    }

    /// <summary>
    /// Явно включает питание из внешнего источника, например из админской консоли.
    /// </summary>
    public async Task StartPowerAsync()
    {
      if (!TryResolveChassisManager() || !ValidatePowerActionAvailability() || taskInProgress)
      {
        return;
      }

      cancellationToken = new CancellationTokenSource();
      taskInProgress = true;
      hasError = false;

      SetLoadingState("Загрузка...", (Color)FindResource("ActiveColor"));

      try
      {
        if (!active)
        {
          await StartPowerSequenceAsync();
        }
        else
        {
          SetConnectedState("Отключить систему");
        }
      }
      finally
      {
        taskInProgress = false;
        SystemStateManager.SetIsActivePower(active);
      }
    }

    /// <summary>
    /// Явно отключает питание из внешнего источника, например из админской консоли.
    /// </summary>
    public async Task StopPowerAsync()
    {
      if (!TryResolveChassisManager() || !ValidatePowerActionAvailability() || taskInProgress)
      {
        return;
      }

      cancellationToken = new CancellationTokenSource();
      taskInProgress = true;
      hasError = false;

      SetLoadingState("Загрузка...", (Color)FindResource("ActiveColor"));

      try
      {
        bool hasPower = active || await model.PowerManager.VerifyPowerAsync(null);
        if (hasPower)
        {
          await StopPowerSequenceAsync();
        }
        else
        {
          SetDisconnectedState("Подключить систему");
        }
      }
      finally
      {
        taskInProgress = false;
        SystemStateManager.SetIsActivePower(active);
      }
    }

    /// <summary>
    /// Начальный процесс включения питания, включая проверку UPS, ожидание загрузки и попытку подключения.
    /// </summary>
    private async Task StartPowerSequenceAsync()
    {
      await EnsureConfiguredUpsPowerAsync();

      if (!await model.PowerManager.VerifyPowerAsync(null))
      {
        await model.PowerManager.StartPowerAsync();
        await ShowCountdownMessageAsync(5, "Питание включено. Ожидание загрузки системы перед инициализацией и сбросом устройств");
      }

      await ResetConfiguredDevicesAfterPowerStartAsync();

      if (!await TryConnectAsync())
      {
        await HandleConnectionErrorAsync();
      }
    }

    private async Task ResetConfiguredDevicesAfterPowerStartAsync()
    {
      var failedDevices = new List<string>();
      var devices = await GetStartupResetDevicesAsync();

      if (devices.Count == 0)
      {
        return;
      }

      MessageEventAdapter.RaiseInfoMessage("Инициализация устройств перед автоматическим сбросом.");

      foreach (var device in devices)
      {
        string deviceName = GetDeviceDisplayName(device);

        try
        {
          MessageEventAdapter.RaiseInfoMessage($"Инициализация устройства: {deviceName}");
          var initialized = await device.ConnectableManager.InitializeAsync();
          if (!initialized.Connect)
          {
            failedDevices.Add($"{deviceName}: {initialized.Answer}");
            continue;
          }

          MessageEventAdapter.RaiseInfoMessage($"Сброс устройства: {deviceName}");
          bool reset = await device.ConnectableManager.ResetAsync();
          if (!reset)
          {
            failedDevices.Add($"{deviceName}: сброс не выполнен");
          }
        }
        catch (Exception ex)
        {
          failedDevices.Add($"{deviceName}: {ex.Message}");
        }
      }

      MessageEventAdapter.RaiseClearMessage();

      if (failedDevices.Count > 0)
      {
        ShowStartupResetWarning(failedDevices);
      }
    }

    private async Task<List<IAttachableDevice>> GetStartupResetDevicesAsync()
    {
      var devices = new List<IAttachableDevice>();
      int numberChassis = model.Number;

      devices.AddRange(await RelaySwitchModules.GetDevicesByNumberChassisAsync(numberChassis));
      devices.AddRange(await SwitchingDevices.GetDevicesByNumberChassisAsync(numberChassis));
      devices.AddRange(await PowerSourceModules.GetDevicesByNumberChassisAsync(numberChassis));
      devices.AddRange(await FastMeters.GetDevicesByNumberChassisAsync(numberChassis));
      devices.AddRange(await BreakdownTesters.GetDevicesByNumberChassisAsync(numberChassis));

      return devices
        .Where(device => device.ConnectableManager != null && device is not DeviceWithCOM)
        .GroupBy(device => (device.DeviceType, device.NumberChassis, device.Number, device.ConnectionDetails))
        .Select(group => group.First())
        .ToList();
    }

    private static string GetDeviceDisplayName(IAttachableDevice device)
    {
      string name = string.IsNullOrWhiteSpace(device.Name)
        ? device.DeviceType.ToString()
        : device.Name;

      return $"{name} №{device.Number} (шасси {device.NumberChassis})";
    }

    private static void ShowStartupResetWarning(IEnumerable<string> failedDevices)
    {
      string devices = string.Join(Environment.NewLine, failedDevices.Select(device => $"• {device}"));
      string message = $"При включении питания не удалось выполнить сброс следующих устройств:{Environment.NewLine}{Environment.NewLine}{devices}{Environment.NewLine}{Environment.NewLine}Питание системы осталось включенным.";

      Application.Current.Dispatcher.Invoke(() =>
        MessageBoxCustom.Show(message, "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning));
    }

    /// <summary>
    /// Процесс отключения системы, включая сброс и остановку питания.
    /// </summary>
    private async Task StopPowerSequenceAsync()
    {
      await UdpBroadcastCommandSender.ResetAllDevicesAsync();
      await Task.Delay(500);

      bool power = true;
      int i = 0;

      while (power && i < 3)
      {
        await model.PowerManager.StopPowerAsync();
        power = await model.PowerManager.VerifyPowerAsync();
        await Task.Delay(100);
        i++;
      }

      if (!power)
      {
        active = false;
        SetDisconnectedState("Подключить систему");
      }
      else
      {
        NotificationHostService.Instance.Show(
              "Отключение системы",
              "Не удалось выполнить отключение питания системы. Проверьте подключение системы к компьютеру и повторите попытку.",
              type: NotificationType.Error);
      }
    }

    /// <summary>
    /// Возвращает configured UPS для текущего шасси и при необходимости включает его.
    /// </summary>
    private async Task EnsureConfiguredUpsPowerAsync()
    {
      IUninterruptiblePowerSupply? ups = GetConfiguredUps();
      if (ups == null)
      {
        return;
      }

      if (!await ups.PowerManager.VerifyPowerAsync())
      {
        await ups.PowerManager.StartPowerAsync();
      }
    }

    /// <summary>
    /// Возвращает настроенный UPS для текущего шасси.
    /// </summary>
    private IUninterruptiblePowerSupply? GetConfiguredUps()
    {
      return UninterruptiblePowerSupplies.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault(device => device.NumberChassis == model.Number);
    }

    /// <summary>
    /// Попытка подключения к системе. При успехе обновляет статус, иначе - вызывает ошибку.
    /// </summary>
    private async Task<bool> TryConnectAsync()
    {
      bool result = await model.PowerManager.VerifyPowerAsync(null);

      if (result)
      {
        SetConnectedState("Отключить систему");
        return true;
      }

      return false;
    }

    /// <summary>
    /// Обработка ошибки подключения с повторными попытками и возможностью отмены.
    /// </summary>
    private async Task HandleConnectionErrorAsync()
    {
      hasError = true;
      SetLoadingState("Отменить подключение", (Color)FindResource("YellowColor"));

      bool exitLoop = false;
      int i = 0;
      do
      {
        if (cancellationToken.Token.IsCancellationRequested)
        {
          exitLoop = true;
        }
        else
        {
          await model.PowerManager.StartPowerAsync();
          await ShowCountdownMessageAsync(3, "Повторная попытка через");
        }

        if (exitLoop)
        {
          break;
        }

        i++;
      }
      while (!await TryConnectAsync() && i < 3);

      if (!exitLoop)
      {
        NotificationHostService.Instance.Show(
               "Включение системы",
               "Не удалось выполнить включение питания системы. Проверьте подключение системы к компьютеру и повторите попытку.",
               type: NotificationType.Error);
      }
    }

    /// <summary>
    /// Отображает сообщение с обратным отсчетом времени.
    /// </summary>
    private async Task ShowCountdownMessageAsync(int seconds, string message)
    {
      for (int i = seconds; i > 0; i--)
      {
        MessageEventAdapter.RaiseInfoMessage($"{message} {i} сек.");
        await Task.Delay(1000);
      }

      MessageEventAdapter.RaiseClearMessage();
    }

    /// <summary>
    /// Установка состояния загрузки с изменением текста и цвета кнопки.
    /// </summary>
    private void SetLoadingState(string text, Color color)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        _ = color;
        SetButtonToolTip(text);
        SetIconForegroundResource("PowerButtonOnForegroundBrush");
        SetIconsState(true);
        PowerActionButton.Opacity = 1;
      });
    }

    /// <summary>
    /// Установка состояния подключения с изменением текста и цвета кнопки.
    /// </summary>
    private void SetConnectedState(string text)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        SetButtonToolTip(text);
        SetIconForegroundResource("PowerButtonOnForegroundBrush");
        SetIconsState(true);
        PowerActionButton.Opacity = 0.92;
        active = true;
      });
    }

    /// <summary>
    /// Установка состояния отключения с изменением текста и цвета кнопки.
    /// </summary>
    private void SetDisconnectedState(string text)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        SetButtonToolTip(text);
        SetIconForegroundResource("PowerButtonOffForegroundBrush");
        SetIconsState(false);
        PowerActionButton.Opacity = 0.86;
        active = false;
      });
    }

    /// <summary>
    /// Переключает видимость иконок питания в зависимости от состояния.
    /// </summary>
    private void SetIconsState(bool isPowerOn)
    {
      if (FindButtonTemplateElement("PowerOnStateIcon") is UIElement powerOnIcon)
      {
        powerOnIcon.Visibility = isPowerOn ? Visibility.Visible : Visibility.Collapsed;
      }

      if (FindButtonTemplateElement("PowerOffStateIcon") is UIElement powerOffIcon)
      {
        powerOffIcon.Visibility = isPowerOn ? Visibility.Collapsed : Visibility.Visible;
      }
    }

    /// <summary>
    /// Устанавливает цвет иконки через ресурс темы.
    /// </summary>
    private void SetIconForegroundResource(string resourceKey)
    {
      if (FindButtonTemplateElement("PowerOnStateIcon") is FrameworkElement powerOnIcon)
      {
        powerOnIcon.SetResourceReference(ForegroundProperty, resourceKey);
      }

      if (FindButtonTemplateElement("PowerOffStateIcon") is FrameworkElement powerOffIcon)
      {
        powerOffIcon.SetResourceReference(ForegroundProperty, resourceKey);
      }
    }

    /// <summary>
    /// Устанавливает текст подсказки кнопки питания.
    /// </summary>
    private void SetButtonToolTip(string text)
    {
      PowerActionButton.ToolTip = text;
    }

    /// <summary>
    /// Загружает менеджер шасси из конфигурации.
    /// </summary>
    private bool TryResolveChassisManager()
    {
      model = ChassisManagers.GetAllAsync().GetAwaiter().GetResult().FirstOrDefault();
      if (model != null)
      {
        return true;
      }

      MessageBoxCustom.Show("Система не задана в конфигурации! Добавьте менеджер шасси в конфигурацию и повторите попытку.", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
      return false;
    }

    /// <summary>
    /// Проверяет доступность реального управления питанием.
    /// </summary>
    private static bool ValidatePowerActionAvailability()
    {
      if (!ExecutionConfig.GetIsIdleModeEnabled())
      {
        return true;
      }

      MessageBoxCustom.Show("Отключите холостой режим для включения питания!", "Ошибка!", MessageBoxButton.OK, image: MessageBoxImage.Error);
      return false;
    }

    /// <summary>
    /// Возвращает элемент шаблона кнопки по имени.
    /// </summary>
    private FrameworkElement FindButtonTemplateElement(string elementName)
    {
      PowerActionButton.ApplyTemplate();
      return PowerActionButton.Template?.FindName(elementName, PowerActionButton) as FrameworkElement;
    }
  }
}
