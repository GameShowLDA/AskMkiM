using Ask.Core.Services.Config.AppSettings;
using Ask.Core.Services.EventCore.Adapters;
using Ask.Core.Services.EventCore.Events;
using Ask.Core.Services.EventCore.Services;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Chassis;
using DataBaseConfiguration.Services.Device;
using Message;
using System.Globalization;
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
      this.Loaded += OnLoaded;
    }

    /// <summary>
    /// Обработка события загрузки, инициализация модели устройства и обновление видимости подсказок.
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      UpdateToolTipVisibility();
      EventAggregator.Subscribe<SystemStateEvents.PowerChanged>(OnPowerChanged);

      this.MouseEnter += OnMouseEnter;
      this.MouseLeave += OnMouseLeave;
      this.PreviewMouseDown += OnPowerButtonClick;
    }

    /// <summary>
    /// Обработка изменения состояния питания для синхронизации с внешними изменениями.
    /// </summary>
    private void OnPowerChanged(SystemStateEvents.PowerChanged e)
    {
      // Получаем новое состояние питания
      bool isPowered = e.IsPowered;

      // Реакция на изменение состояния
      if (isPowered)
        Console.WriteLine("⚡ Питание включено");
      else
        Console.WriteLine("❌ Питание отключено");
    }

    /// <summary>
    /// Анимация для плавного увеличения непрозрачности кнопки при наведении курсора.
    /// </summary>
    private async void OnMouseEnter(object sender, MouseEventArgs e)
    {
      if (!taskInProgress || hasError)
      {
        await AnimateOpacityAsync(1);
      }
    }

    /// <summary>
    /// Анимация для плавного уменьшения непрозрачности кнопки при уходе курсора.
    /// </summary>
    private async void OnMouseLeave(object sender, MouseEventArgs e)
    {
      if (!taskInProgress || hasError)
      {
        await AnimateOpacityAsync(0.5);
      }
    }

    /// <summary>
    /// Обработчик события клика по кнопке питания.
    /// Запускает процесс включения/выключения питания в зависимости от текущего состояния.
    /// </summary>
    /// <param name="sender">Источник события (кнопка питания).</param>
    /// <param name="e">Аргументы события мыши.</param>
    public async void OnPowerButtonClick(object sender, MouseButtonEventArgs e)
    {
      await PowerButtonClick();
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки питания. Выполняет последовательность включения или выключения питания системы.
    /// Если система не задана в конфигурации или включен холостой режим, выводит сообщение об ошибке.
    /// Если задача уже выполняется, отменяет выполнение текущей задачи.
    /// </summary>
    /// <returns>Асинхронная задача.</returns>
    public async Task PowerButtonClick()
    {
      model = new ChassisManagerServices().GetAll().FirstOrDefault();

      if (model == null)
      {
        MessageBoxCustom.Show("Система не задана в конфигурации! Добавьте менеджер шасси в конфигурацию и повторите попытку.", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      if (ExecutionConfig.GetIsIdleModeEnabled())
      {
        MessageBoxCustom.Show("Отключите холостой режим для включения питания!", "Ошибка!", MessageBoxButton.OK, image: MessageBoxImage.Error);
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

      if (!active)
      {
        await StartPowerSequenceAsync();
      }
      else
      {
        await StopPowerSequenceAsync();
      }

      taskInProgress = false;
      await SystemStateManager.SetIsActivePower(active);
    }

    /// <summary>
    /// Начальный процесс включения питания, включая ожидание загрузки и попытку подключения.
    /// </summary>
    private async Task StartPowerSequenceAsync()
    {
      await model.PowerManager.StartPowerAsync();
      await ShowCountdownMessageAsync(5, "Ожидание загрузки системы");

      if (!await TryConnectAsync())
      {
        await HandleConnectionErrorAsync();
      }
    }

    /// <summary>
    /// Процесс отключения системы, включая сброс и остановку питания.
    /// </summary>
    private async Task StopPowerSequenceAsync()
    {
      active = false;
      await NewCore.Communication.DeviceCommandSender.ResetAllSystem();

      await Task.Delay(1000);

      await model.PowerManager.StopPowerAsync();
      await Task.Delay(1000);

      SetDisconnectedState("Подключить систему");
    }

    /// <summary>
    /// Попытка подключения к системе. При успехе обновляет статус, иначе - вызывает ошибку.
    /// </summary>
    private async Task<bool> TryConnectAsync()
    {
      var result = await model.ConnectableManager.InitializeAsync(null);

      if (result.Item1)
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
      }
      while (!await TryConnectAsync());
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
    /// Анимация плавного изменения непрозрачности кнопки до заданного уровня.
    /// </summary>
    private async Task AnimateOpacityAsync(double targetOpacity)
    {
      while (Math.Abs(GridBlock.Opacity - targetOpacity) > 0.1)
      {
        GridBlock.Opacity += (targetOpacity - GridBlock.Opacity) * 0.1;
        await Task.Delay(10);
      }

      GridBlock.Opacity = targetOpacity;
    }

    /// <summary>
    /// Обновление видимости всплывающей подсказки, если текст не помещается.
    /// </summary>
    private void UpdateToolTipVisibility()
    {
      var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
      var formattedText = new FormattedText(
          nameTextBlock.Text,
          CultureInfo.CurrentCulture,
          FlowDirection.LeftToRight,
          new Typeface(nameTextBlock.FontFamily, nameTextBlock.FontStyle, nameTextBlock.FontWeight, nameTextBlock.FontStretch),
          nameTextBlock.FontSize,
          nameTextBlock.Foreground,
          pixelsPerDip);

      nameTextBlock.ToolTip = formattedText.Width > nameTextBlock.ActualWidth ? nameTextBlock.Text : null;
    }

    /// <summary>
    /// Установка состояния загрузки с изменением текста и цвета кнопки.
    /// </summary>
    private void SetLoadingState(string text, Color color)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        nameTextBlock.Text = text;
        GridBlock.SetResourceReference(BackgroundProperty, color);
        nameTextBlock.Foreground = (SolidColorBrush)Application.Current.Resources["ForegroundSolidColorBrush"];
        GridBlock.Opacity = 1;
      });
    }

    /// <summary>
    /// Установка состояния подключения с изменением текста и цвета кнопки.
    /// </summary>
    private void SetConnectedState(string text)
    {
      Application.Current.Dispatcher.Invoke(() =>
      {
        nameTextBlock.Text = text;
        GridBlock.SetResourceReference(BackgroundProperty, "GreenColorSolidColorBrush");
        nameTextBlock.Foreground = (SolidColorBrush)Application.Current.Resources["ActiveForegroundSolidColorBrush"];
        GridBlock.Opacity = 0.5;
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
        nameTextBlock.Text = text;
        GridBlock.SetResourceReference(BackgroundProperty, "RedColorSolidColorBrush");
        nameTextBlock.Foreground = (SolidColorBrush)Application.Current.Resources["ForegroundSolidColorBrush"];
        GridBlock.Opacity = 0.5;
        active = false;
      });
    }
  }
}
