using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Interfaces.DeviceInterfaces.Multimeter;
using Ask.LogLib;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ask.UI.Features.ServiceTools.Multimeter
{
  /// <summary>
  /// Предоставляет сервисное управление мультиметрами и ручными измерениями.
  /// </summary>
  public partial class MultimeterControl : UserControl
  {
    private static readonly string[] Modes =
      ["Сопротивление", "Напряжение AC", "Напряжение DC", "Ёмкость", "Прозвонка", "Диод"];
    private readonly Func<Task<IReadOnlyList<IMultimeter>>> metersProvider;
    private bool operationInProgress;

    /// <summary>
    /// Инициализирует панель управления мультиметрами.
    /// </summary>
    /// <param name="metersProvider">Функция получения мультиметров первого шасси.</param>
    public MultimeterControl(Func<Task<IReadOnlyList<IMultimeter>>> metersProvider)
    {
      this.metersProvider = metersProvider ?? throw new ArgumentNullException(nameof(metersProvider));
      InitializeComponent();
      ModeComboBox.ItemsSource = Modes;
      ModeComboBox.SelectedIndex = 0;
      Loaded += MultimeterControl_Loaded;
    }

    private IMultimeter Meter => MeterComboBox.SelectedItem as IMultimeter
      ?? throw new InvalidOperationException("Выберите мультиметр.");

    private async void MultimeterControl_Loaded(object sender, RoutedEventArgs e)
    {
      Loaded -= MultimeterControl_Loaded;
      await LoadMetersAsync();
    }

    private async Task LoadMetersAsync()
    {
      try
      {
        var meters = await metersProvider();
        MeterComboBox.ItemsSource = meters;
        MeterComboBox.SelectedIndex = meters.Count > 0 ? 0 : -1;
        UpdateStatus();
      }
      catch (Exception exception)
      {
        MeterComboBox.ItemsSource = null;
        StatusText.Text = "Не удалось загрузить мультиметры. Подробности записаны в консоль.";
        StatusIndicator.Background = Brushes.IndianRed;
        ReportError("загрузка приборов", exception);
      }
    }

    private void UpdateStatus()
    {
      if (MeterComboBox.SelectedItem is not IMultimeter meter)
      {
        StatusText.Text = "Мультиметры не найдены в конфигурации первого шасси.";
        StatusIndicator.Background = Brushes.IndianRed;
        return;
      }

      StatusText.Text = $"{meter.Name}, №{meter.Number} · {meter.ConnectionDetails} · {meter.ConnectionInfo.GetConnectionStatus()}";
      StatusIndicator.Background = meter.ConnectionInfo.IsConnected ? Brushes.MediumSeaGreen : Brushes.Goldenrod;
    }

    private async Task ExecuteAsync(string operation, Func<IMultimeter, Task<bool>> action)
    {
      if (operationInProgress)
      {
        return;
      }

      try
      {
        operationInProgress = true;
        IsEnabled = false;
        if (!await action(Meter))
        {
          LoggerUtility.LogError(
            $"Мультиметр — {operation}: прибор вернул отрицательный результат.",
            isDeviceLog: true);
          UpdateStatus();
          return;
        }

        LoggerUtility.LogInformation($"Мультиметр — {operation}: выполнено.", isDeviceLog: true);
        UpdateStatus();
      }
      catch (Exception exception)
      {
        ReportError(operation, exception);
      }
      finally
      {
        IsEnabled = true;
        operationInProgress = false;
      }
    }

    private static double Value(TextBox textBox, string name)
    {
      string text = textBox.Text.Trim().Replace(',', '.');
      if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
      {
        throw new InvalidOperationException($"Укажите корректное значение «{name}».");
      }

      return value;
    }

    private string Mode => ModeComboBox.SelectedItem as string
      ?? throw new InvalidOperationException("Выберите режим измерения.");

    private Task<bool> SetModeAsync(IMultimeter meter) => Mode switch
    {
      "Сопротивление" => meter.ResistanceManager.SetResistanceModeAsync(),
      "Напряжение AC" => meter.AcVoltageManager.SetACVoltageModeAsync(),
      "Напряжение DC" => meter.DcVoltageManager.SetDCVoltageModeAsync(),
      "Ёмкость" => meter.CapacitanceManager.SetCapacitanceModeAsync(),
      "Прозвонка" => meter.ContinuityManager.SetContinuityModeAsync(),
      "Диод" => meter.DiodeManager.SetDiodeModeAsync(),
      _ => throw new InvalidOperationException("Неизвестный режим измерения.")
    };

    private Task<bool> SetRangeAsync(IMultimeter meter)
    {
      double range = Value(RangeTextBox, "диапазон");
      return Mode switch
      {
        "Сопротивление" => meter.ResistanceManager.SetResistanceRangeAsync(range),
        "Напряжение AC" => meter.AcVoltageManager.SetACVoltageRangeAsync(range),
        "Напряжение DC" => meter.DcVoltageManager.SetDCVoltageRangeAsync(range),
        "Ёмкость" => meter.CapacitanceManager.SetCapacitanceRangeAsync(range),
        _ => throw new InvalidOperationException("Для выбранного режима отдельная установка диапазона не поддерживается.")
      };
    }

    private async Task<double> MeasureAsync(IMultimeter meter, MeasurementRange range) => Mode switch
    {
      "Сопротивление" => await meter.ResistanceManager.MeasureResistanceAsync(range),
      "Напряжение AC" => await meter.AcVoltageManager.MeasureACVoltageAsync(range),
      "Напряжение DC" => await meter.DcVoltageManager.MeasureDCVoltageAsync(range),
      "Ёмкость" => await meter.CapacitanceManager.MeasureCapacitanceAsync(range),
      "Прозвонка" => await meter.ContinuityManager.CheckContinuityAsync(range),
      "Диод" => await meter.DiodeManager.CheckDiodeAsync(range),
      _ => throw new InvalidOperationException("Неизвестный режим измерения.")
    };

    private async void MeasureButton_Click(object sender, RoutedEventArgs e)
    {
      if (operationInProgress)
      {
        return;
      }

      try
      {
        operationInProgress = true;
        IsEnabled = false;
        double target = Value(TargetTextBox, "ожидаемое значение");
        double lower = Value(LowerTextBox, "минимум");
        double upper = Value(UpperTextBox, "максимум");
        if (lower > upper)
        {
          throw new InvalidOperationException("Минимальная граница не может быть больше максимальной.");
        }

        double result = await MeasureAsync(Meter, new MeasurementRange(target, lower, upper));
        ResultValueText.Text = result.ToString("G10", CultureInfo.CurrentCulture);
        ResultDetailsText.Text = $"{Mode} · допустимый диапазон: {lower:G6}…{upper:G6}";
        LoggerUtility.LogInformation($"Мультиметр — {Mode}: результат {result:G10}.", isDeviceLog: true);
      }
      catch (Exception exception)
      {
        ResultValueText.Text = "Ошибка";
        ResultDetailsText.Text = exception.Message;
        ReportError("измерение", exception);
      }
      finally
      {
        IsEnabled = true;
        operationInProgress = false;
      }
    }

    private static void ReportError(string operation, Exception exception) =>
      LoggerUtility.LogError($"Мультиметр — {operation}: {exception.Message}", isDeviceLog: true);

    private async void RefreshButton_Click(object sender, RoutedEventArgs e) => await LoadMetersAsync();
    private void MeterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateStatus();
    private async void ConnectButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("подключение", async m => (await m.ConnectableManager.ConnectAsync()).Connect);
    private async void InitializeButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("инициализация", async m => (await m.ConnectableManager.InitializeAsync()).Connect);
    private async void DisconnectButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("отключение", m => m.ConnectableManager.DisconnectAsync());
    private async void ResetButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync("сброс", m => m.ConnectableManager.ResetAsync());
    private async void SetModeButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync($"установка режима «{Mode}»", SetModeAsync);
    private async void SetRangeButton_Click(object sender, RoutedEventArgs e) =>
      await ExecuteAsync($"установка диапазона режима «{Mode}»", SetRangeAsync);
  }
}
