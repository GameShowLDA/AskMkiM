using Message;
using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.GPT.Mode
{
  /// <summary>
  /// Компонент для управления режимом DCW.
  /// При инициализации устанавливается режим DCW и загружается конфигурация устройства.
  /// </summary>
  public partial class DcwMode : UserControl
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="DcwMode"/>.
    /// При инициализации устанавливается режим DCW и запускается загрузка конфигурации устройства.
    /// </summary>
    public DcwMode()
    {
      InitializeComponent();
    }

    /// <summary>
    /// Асинхронно загружает конфигурацию устройства и обновляет элементы управления.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию загрузки конфигурации.</returns>
    private async Task LoadConfigurationAsync()
    {
      try
      {
        var systemData = await GPTPunchControl.ModelGPT.DcwManger.Config.ReadConfigurationAsync();

        VoltageSlider.Value = systemData.Voltage;
        ChiSlider.Value = systemData.HighCurrentLimit;
        CloSlider.Value = systemData.LowCurrentLimit;
        TimeSlider.Value = systemData.TestTime;
        RefSlider.Value = systemData.Offset;
        ArcCurrentSlider.Value = systemData.ArcCurrent;
        RampSlider.Value = systemData.RampTime;

        LastReadTimeText.Text = $"Дата и время: {DateTime.Now}";
        VoltageValueText.Text = $"Напряжение DCW: {systemData.Voltage:F3} кВ";
        ChiValueText.Text = $"Высокий предел тока DCW: {systemData.HighCurrentLimit:F3} мА";
        CloValueText.Text = $"Низкий предел тока DCW: {systemData.LowCurrentLimit:F3} мА";
        TimeValueText.Text = $"Время теста DCW: {systemData.TestTime:F1} сек";
        RefValueText.Text = $"Смещение DCW: {systemData.Offset:F3} мА";
        ArcCurrentValueText.Text = $"Текущее значение тока DCW: {systemData.ArcCurrent:F3} мА";
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show($"Ошибка при считывании конфигурации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /// <summary>
    /// Обрабатывает нажатие на кнопку для считывания конфигурации.
    /// Загружает конфигурацию с устройства и обновляет элементы управления.
    /// </summary>
    /// <param name="sender">Источник события (кнопка).</param>
    /// <param name="e">Данные события.</param>
    private async void ReadConfigurationButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        var systemData = await GPTPunchControl.ModelGPT.DcwManger.Config.ReadConfigurationAsync();
        LastReadTimeText.Text = $"Дата и время: {DateTime.Now}";
        VoltageValueText.Text = $"Напряжение ACW: {systemData.Voltage:F3} кВ";
        ChiValueText.Text = $"Высокий предел тока ACW: {systemData.HighCurrentLimit:F3} мА";
        CloValueText.Text = $"Низкий предел тока ACW: {systemData.LowCurrentLimit:F3} мА";
        TimeValueText.Text = $"Время теста ACW: {systemData.TestTime:F1} сек";
        RefValueText.Text = $"Смещение ACW: {systemData.Offset:F3} мА";
        ArcCurrentValueText.Text = $"Текущее значение тока ACW: {systemData.ArcCurrent:F3} мА";
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show($"Ошибка при считывании конфигурации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /// <summary>
    /// Обрабатывает нажатие на кнопку для запуска теста.
    /// Запускает тест устройства и отображает результат измерения тока.
    /// </summary>
    /// <param name="sender">Источник события (кнопка).</param>
    /// <param name="e">Данные события.</param>
    private async void StartTestButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        double result = (await GPTPunchControl.ModelGPT.DcwManger.Measure.MeasureAsync()).value;
        TestResultText.Text = $"Результат теста: {result:F3} мА";
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show($"Ошибка при запуске теста: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private bool connect = false;
    private async void Button_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (!connect)
      {
        var mode = await GPTPunchControl.ModelGPT.DcwManger.Mode.SetModeAsync();
        if (mode.Success)
        {
          PanelManagment.Visibility = Visibility.Visible;
          await LoadConfigurationAsync();
          connect = true;
          ConnectButton.Content = "Отключить режим DCW";
        }
      }
      else
      {
        PanelManagment.Visibility = Visibility.Collapsed;
        ConnectButton.Content = "Включить режим DCW";
        connect = false;
      }
    }

    private async void Button_PreviewMouseDown_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      // Сначала собираем значения с UI строго в UI-потоке
      double voltage = 0, chi = 0, clo = 0, time = 0, timeRamp = 0, refValue = 0, arcCurrent = 0;

      Dispatcher.Invoke(() =>
      {
        voltage = Math.Round(VoltageSlider.Value, 3);
        chi = Math.Round(ChiSlider.Value, 3);
        clo = Math.Round(CloSlider.Value, 3);
        time = Math.Round(TimeSlider.Value, 1);
        timeRamp = Math.Round(RampSlider.Value, 1);
        refValue = Math.Round(RefSlider.Value, 3);
        arcCurrent = Math.Round(ArcCurrentSlider.Value, 3);
      });

      await GPTPunchControl.ModelGPT.DcwManger.Voltage.SetVoltageAsync(voltage).ConfigureAwait(false);
      await GPTPunchControl.ModelGPT.DcwManger.Time.SetTestTimeAsync(time).ConfigureAwait(false);
      await GPTPunchControl.ModelGPT.DcwManger.Time.SetRampTimeAsync(timeRamp).ConfigureAwait(false);
      await GPTPunchControl.ModelGPT.DcwManger.Offset.SetOffsetAsync(refValue).ConfigureAwait(false);
      await GPTPunchControl.ModelGPT.DcwManger.ArcCurrent.SetArcCurrentAsync(arcCurrent).ConfigureAwait(false);
      await GPTPunchControl.ModelGPT.DcwManger.CurrentLimits.SetLowCurrentLimitAsync(clo).ConfigureAwait(false);
      await GPTPunchControl.ModelGPT.DcwManger.CurrentLimits.SetHighCurrentLimitAsync(chi).ConfigureAwait(false);

    }
  }
}
