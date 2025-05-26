using System.Windows;
using System.Windows.Controls;
using Utilities;

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
      _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
      try
      {
        await GPTPunchControl.ModelGPT.AcwManger.SetModeAsync();
        await LoadConfigurationAsync();
      }
      catch (Exception ex)
      {
        LoggerUtility.LogException($"Ошибка в {nameof(InitializeAsync)}", ex);
      }
    }

    /// <summary>
    /// Асинхронно загружает конфигурацию устройства и обновляет элементы управления.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    private async Task LoadConfigurationAsync()
    {
      try
      {
        var systemData = await GPTPunchControl.ModelGPT.AcwManger.ReadConfigurationAsync();

        VoltageSlider.Value = systemData.Voltage;
        ChiSlider.Value = systemData.HighCurrentLimit;
        CloSlider.Value = systemData.LowCurrentLimit;
        TimeSlider.Value = systemData.TestTime;
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
        MessageBox.Show($"Ошибка при считывании конфигурации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /// <summary>
    /// Обработчик изменения значения слайдера для напряжения.
    /// Измеряет новое значение напряжения и отправляет его на устройство.
    /// </summary>
    private async void VoltageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double voltage = Math.Round(VoltageSlider.Value, 3);
      await GPTPunchControl.ModelGPT.AcwManger.SetVoltageAsync(voltage);
    }

    /// <summary>
    /// Обработчик изменения значения слайдера для высокого предела тока.
    /// Измеряет новое значение и отправляет его на устройство.
    /// </summary>
    private async void ChiSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double chi = Math.Round(ChiSlider.Value, 3);
      await GPTPunchControl.ModelGPT.AcwManger.SetHighCurrentLimitAsync(chi);
    }

    /// <summary>
    /// Обработчик изменения значения слайдера для низкого предела тока.
    /// Измеряет новое значение и отправляет его на устройство.
    /// </summary>
    private async void CloSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double clo = Math.Round(CloSlider.Value, 3);
      await GPTPunchControl.ModelGPT.AcwManger.SetLowCurrentLimitAsync(clo);
    }

    /// <summary>
    /// Обработчик изменения значения слайдера для времени теста.
    /// Измеряет новое значение времени и отправляет его на устройство.
    /// </summary>
    private async void TimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double time = Math.Round(TimeSlider.Value, 1);
      await GPTPunchControl.ModelGPT.AcwManger.SetTestTimeAsync(time);
    }

    /// <summary>
    /// Обработчик изменения значения слайдера для смещения.
    /// Измеряет новое значение смещения и отправляет его на устройство.
    /// </summary>
    private async void RefSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double refValue = Math.Round(RefSlider.Value, 3);
      await GPTPunchControl.ModelGPT.AcwManger.SetOffsetAsync(refValue);
    }

    /// <summary>
    /// Обработчик изменения значения слайдера для тока дуги.
    /// Измеряет новое значение тока дуги и отправляет его на устройство.
    /// </summary>
    private async void ArcCurrentSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      if (ArcCurrentSlider != null)
      {
        double arcCurrent = Math.Round(ArcCurrentSlider.Value, 3);
        await GPTPunchControl.ModelGPT.AcwManger.SetArcCurrentAsync(arcCurrent);
      }
    }

    /// <summary>
    /// Обработчик изменения выбранного элемента в ComboBox для частоты.
    /// При выборе обновляет частоту устройства.
    /// </summary>
    private async void FrequencyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (FrequencyComboBox.SelectedItem is ComboBoxItem selectedItem)
      {
        try
        {
          string frequencyText = selectedItem.Content.ToString();
          if (double.TryParse(frequencyText.Replace("Гц", "").Trim(), out double frequency))
          {
            await GPTPunchControl.ModelGPT.AcwManger.SetFrequencyAsync((int)frequency).ConfigureAwait(false);
            Dispatcher.Invoke(() =>
            {
              FrequencyValueText.Text = $"Частота ACW: {frequency} Гц";
            });
          }
        }
        catch (Exception ex)
        {
          Dispatcher.Invoke(() =>
          {
            MessageBox.Show($"Ошибка при установке частоты: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
          });
        }
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
        var systemData = await GPTPunchControl.ModelGPT.AcwManger.ReadConfigurationAsync();
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
        MessageBox.Show($"Ошибка при считывании конфигурации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
        double result = await GPTPunchControl.ModelGPT.AcwManger.MeasureCurrentAsync();
        TestResultText.Text = $"Результат теста: {result:F3} мА";
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при запуске теста: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
  }
}
