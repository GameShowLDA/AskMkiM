using Ask.Core.Shared.Interfaces.DeviceInterfaces.BreakdownTester;
using Message;
using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.GPT.Mode
{
  /// <summary>
  /// Компонент для работы с режимом ACW.
  /// При инициализации устанавливает режим ACW и загружает конфигурацию устройства.
  /// </summary>
  public partial class AcwMode : UserControl
  {
    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AcwMode"/>.
    /// При инициализации устанавливается режим ACW и запускается загрузка конфигурации.
    /// </summary>
    public AcwMode()
    {
      InitializeComponent();
    }

    private bool connect = false;


    /// <summary>
    /// Асинхронно загружает конфигурацию устройства и обновляет элементы управления.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    private async Task LoadConfigurationAsync()
    {
      try
      {
        var systemData = await GPTPunchControl.ModelGPT.AcwManger.Config.ReadConfigurationAsync();

        VoltageSlider.Value = systemData.Voltage;
        ChiSlider.Value = systemData.HighCurrentLimit;
        CloSlider.Value = systemData.LowCurrentLimit;
        TimeSlider.Value = systemData.TestTime;
        RampTimeSlider.Value = systemData.RampTime;
        FrequencyComboBox.SelectedIndex = systemData.Frequency == 50 ? 0 : 1;
        RefSlider.Value = systemData.Offset;
        ArcCurrentSlider.Value = systemData.ArcCurrent;

        LastReadTimeText.Text = $"Дата и время: {DateTime.Now}";
        VoltageValueText.Text = $"Напряжение ACW: {systemData.Voltage:F3} кВ";
        ChiValueText.Text = $"Высокий предел тока ACW: {systemData.HighCurrentLimit:F3} мА";
        CloValueText.Text = $"Низкий предел тока ACW: {systemData.LowCurrentLimit:F3} мА";
        TimeValueText.Text = $"Время теста ACW: {systemData.TestTime:F1} сек";
        FrequencyValueText.Text = $"Частота ACW: {systemData.Frequency} Гц";
        RefValueText.Text = $"Смещение ACW: {systemData.Offset:F3} мА";
        ArcCurrentValueText.Text = $"Текущее значение тока ACW: {systemData.ArcCurrent:F3} мА";
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show($"Ошибка при считывании конфигурации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /// <summary>
    /// Обработчик нажатия на кнопку для считывания конфигурации.
    /// Обновляет элементы управления с текущими значениями конфигурации устройства.
    /// </summary>
    private async void ReadConfigurationButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        var systemData = await GPTPunchControl.ModelGPT.AcwManger.Config.ReadConfigurationAsync();
        LastReadTimeText.Text = $"Дата и время: {DateTime.Now}";
        VoltageValueText.Text = $"Напряжение ACW: {systemData.Voltage:F3} кВ";
        ChiValueText.Text = $"Высокий предел тока ACW: {systemData.HighCurrentLimit:F3} мА";
        CloValueText.Text = $"Низкий предел тока ACW: {systemData.LowCurrentLimit:F3} мА";
        TimeValueText.Text = $"Время теста ACW: {systemData.TestTime:F1} сек";
        FrequencyValueText.Text = $"Частота ACW: {systemData.Frequency} Гц";
        RefValueText.Text = $"Смещение ACW: {systemData.Offset:F3} мА";
        ArcCurrentValueText.Text = $"Текущее значение тока ACW: {systemData.ArcCurrent:F3} мА";
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show($"Ошибка при считывании конфигурации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /// <summary>
    /// Обработчик нажатия на кнопку для запуска теста.
    /// Запускает измерение тока ACW и выводит результат.
    /// </summary>
    private async void StartTestButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        double result = (await GPTPunchControl.ModelGPT.AcwManger.Measure.MeasureAsync()).value;
        TestResultText.Text = $"Результат теста: {result:F3} мА";
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show($"Ошибка при запуске теста: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void Button_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (!connect)
      {
        var mode = await GPTPunchControl.ModelGPT.AcwManger.Mode.SetModeAsync();
        if (mode.Success)
        {
          PanelManagment.Visibility = Visibility.Visible;
          await LoadConfigurationAsync();
          connect = true;
          ConnectButton.Content = "Отключить режим ACW";
        }
      }
      else
      {
        PanelManagment.Visibility = Visibility.Collapsed;
        ConnectButton.Content = "Включить режим ACW";
        connect = false;
      }
    }

    private async void Button_PreviewMouseDown_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      double voltage = 0, chi = 0, clo = 0, time = 0, timeRamp = 0, refValue = 0, arcCurrent = 0;
      int frequency = 50;

      Dispatcher.Invoke(() =>
      {
        voltage = Math.Round(VoltageSlider.Value, 3);
        chi = Math.Round(ChiSlider.Value, 3);
        clo = Math.Round(CloSlider.Value, 3);
        time = Math.Round(TimeSlider.Value, 1);
        timeRamp = Math.Round(RampTimeSlider.Value, 1);
        refValue = Math.Round(RefSlider.Value, 3);
        arcCurrent = Math.Round(ArcCurrentSlider.Value, 3);

        if (FrequencyComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
          string frequencyText = selectedItem.Content.ToString();
          if (double.TryParse(frequencyText.Replace("Гц", "").Trim(), out double freq))
            frequency = (int)freq;
        }
      });

      await GPTPunchControl.ModelGPT.AcwManger.Voltage.SetVoltageAsync(voltage);
      await GPTPunchControl.ModelGPT.AcwManger.Time.SetTestTimeAsync(time);
      await GPTPunchControl.ModelGPT.AcwManger.Time.SetRampTimeAsync(timeRamp).ConfigureAwait(false);
      await GPTPunchControl.ModelGPT.AcwManger.FrequencyConfigurable.SetFrequencyAsync(frequency).ConfigureAwait(false);
      await GPTPunchControl.ModelGPT.AcwManger.CurrentLimits.SetHighCurrentLimitAsync(chi);
      await GPTPunchControl.ModelGPT.AcwManger.CurrentLimits.SetLowCurrentLimitAsync(clo).ConfigureAwait(false);
      await GPTPunchControl.ModelGPT.AcwManger.Offset.SetOffsetAsync(refValue).ConfigureAwait(false);
      await GPTPunchControl.ModelGPT.AcwManger.ArcCurrent.SetArcCurrentAsync(arcCurrent).ConfigureAwait(false);
    }
  }
}
