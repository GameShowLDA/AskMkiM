using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Core.ConfigCollector;
using static AppConfig.Config.ExecutionConfig;
using static AppConfig.Config.SystemStateManager;
using static AppConfig.EventAggregator;

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
    private Core.ManagerShassy.Model model;

    /// <summary>
    /// Флаг, указывающий, активно ли подключение системы (true - подключено, false - отключено).
    /// </summary>
    private bool active = false;

    /// <summary>
    /// Статический флаг, показывающий, выполняется ли в данный момент задача подключения/отключения.
    /// </summary>
    private static bool taskInProgress = false;

    /// <summary>
    /// Статический флаг, указывающий на наличие ошибки подключения.
    /// </summary>
    private static bool hasError = false;

    /// <summary>
    /// Ссылка на верхнюю панель для отображения информационных сообщений.
    /// </summary>
    private TopPanelControl topPanel;

    /// <summary>
    /// Токен для отмены асинхронных задач при необходимости.
    /// </summary>
    private CancellationTokenSource cancellationToken;

    /// <summary>
    /// Цвет рамки в состоянии загрузки.
    /// </summary>
    Color ActiveBorder => (Color)ColorConverter.ConvertFromString("#1ca3e9");

    /// <summary>
    /// Цвет при успешном подключении.
    /// </summary>
    Color Connected => (Color)ColorConverter.ConvertFromString("#4b7765");

    /// <summary>
    /// Цвет для отображения ошибок.
    /// </summary>
    Color Error => (Color)ColorConverter.ConvertFromString("#b23a48");
    #endregion

    /// <summary>
    /// Инициализация PowerButton, подписка на события и настройки поведения кнопки.
    /// </summary>
    public PowerButton()
    {
      InitializeComponent();
      PowerChanged += OnPowerChanged;
      this.Loaded += OnLoaded;
      this.MouseEnter += OnMouseEnter;
      this.MouseLeave += OnMouseLeave;
      this.PreviewMouseDown += OnPowerButtonClick;
    }

    /// <summary>
    /// Обработка события загрузки, инициализация модели устройства и обновление видимости подсказок.
    /// </summary>
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      UpdateToolTipVisibility();
    }

    /// <summary>
    /// Обработка изменения состояния питания для синхронизации с внешними изменениями.
    /// </summary>
    private void OnPowerChanged(bool newValue)
    {
      if (active != newValue)
      {
        OnPowerButtonClick(null, null);
        active = newValue;
      }
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
    /// Включает или выключает питание в зависимости от текущего состояния системы.
    /// Выполняет серию проверок, таких как наличие модели шасси, включение холостого режима и наличие активной задачи.
    /// </summary>
    public async Task PowerButtonClick()
    {
      model = ConfigCollector.GetManagerShassy();

      if (model == null)
      {
        MessageBox.Show("Система не задана в конфигурации! Добавьте менеджер шасси в конфигурацию и повторите попытку.", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      if (await GetIsIdleModeEnabled())
      {
        MessageBox.Show("Отключите холостой режим для включения питания!", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      if (taskInProgress)
      {
        cancellationToken.Cancel();
        return;
      }

      cancellationToken = new CancellationTokenSource();
      taskInProgress = true;
      hasError = false;

      SetLoadingState("Загрузка...", ActiveBorder);

      if (!active)
      {
        await StartPowerSequenceAsync();
      }
      else
      {
        await StopPowerSequenceAsync();
      }

      taskInProgress = false;
      await SetIsActivePower(active);
    }

    /// <summary>
    /// Начальный процесс включения питания, включая ожидание загрузки и попытку подключения.
    /// </summary>
    private async Task StartPowerSequenceAsync()
    {
      if (topPanel == null)
      {
        topPanel = new TopPanelControl();
      }

      await Core.ManagerShassy.Function.StartPowerAsync(model.IPAddress);
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
      await Core.Communication.CommunicationManager.ResetAllSystem();

      await Task.Delay(1000);

      await Core.ManagerShassy.Function.StopPowerAsync(model.IPAddress);
      await Task.Delay(1000);

      SetDisconnectedState("Подключить систему", Error);
    }

    /// <summary>
    /// Попытка подключения к системе. При успехе обновляет статус, иначе - вызывает ошибку.
    /// </summary>
    private async Task<bool> TryConnectAsync()
    {
      var result = await Core.ManagerShassy.Function.Initialize(model.IPAddress);

      if (result.Item1)
      {
        SetConnectedState("Отключить систему", Connected);
        return true;
      }

      SetDisconnectedState("Подключить систему", Error);
      return false;
    }

    /// <summary>
    /// Обработка ошибки подключения с повторными попытками и возможностью отмены.
    /// </summary>
    private async Task HandleConnectionErrorAsync()
    {
      hasError = true;
      SetLoadingState("Отменить подключение", Error);

      bool exitLoop = false;

      do
      {
        if (cancellationToken.Token.IsCancellationRequested)
        {
          exitLoop = true;
        }
        else
        {
          await Core.ManagerShassy.Function.StartPowerAsync(model.IPAddress);
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
        await Task.Delay(1000);
      }
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
      nameTextBlock.Text = text;
      GridBlock.Background = new SolidColorBrush(color);
      nameTextBlock.Foreground = (SolidColorBrush)Application.Current.Resources["ForegroundSolidColorBrush"];
      GridBlock.Opacity = 1;
    }

    /// <summary>
    /// Установка состояния подключения с изменением текста и цвета кнопки.
    /// </summary>
    private void SetConnectedState(string text, Color color)
    {
      nameTextBlock.Text = text;
      GridBlock.Background = new SolidColorBrush(color);
      nameTextBlock.Foreground = (SolidColorBrush)Application.Current.Resources["ActiveForegroundSolidColorBrush"];
      GridBlock.Opacity = 0.5;
      active = true;
    }

    /// <summary>
    /// Установка состояния отключения с изменением текста и цвета кнопки.
    /// </summary>
    private void SetDisconnectedState(string text, Color color)
    {
      nameTextBlock.Text = text;
      GridBlock.Background = new SolidColorBrush(color);
      nameTextBlock.Foreground = (SolidColorBrush)Application.Current.Resources["PrimarySolidColorBrush"];
      GridBlock.Opacity = 0.5;
      active = false;
    }

    /// <summary>
    /// Устанавливает ссылку на верхнюю панель для вывода сообщений.
    /// </summary>
    /// <param name="topPanel">Ссылка на объект верхней панели, которая будет использоваться для вывода сообщений.</param>
    /// <remarks>
    /// Этот метод позволяет связать верхнюю панель с текущим компонентом, чтобы позднее использовать её для
    /// вывода сообщений или других операций, связанных с интерфейсом.
    /// </remarks>
    public void SetTopPanel(TopPanelControl topPanel)
    {
      this.topPanel = topPanel;
    }
  }
}
