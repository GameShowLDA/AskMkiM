using Message;
using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.GPT.Mode
{
  public partial class IrMode : UserControl
  {
    /// <summary>
    /// Компонент для управления режимом Ir.
    /// При инициализации устанавливается режим Ir и загружается конфигурация устройства.
    /// </summary>
    public IrMode()
    {
      InitializeComponent();
    }

    private bool connect = false;


    /// <summary>
    /// Метод для загрузки конфигурации и заполнения элементов управления.
    /// </summary>
    private async Task LoadConfigurationAsync()
    {
      try
      {
        var systemData = await GPTPunchControl.ModelGPT.IrManger.Config.ReadConfigurationAsync();

        // Заполняем элементы управления
        VoltageSlider.Value = systemData.Voltage / 1000.0; // Переводим напряжение из В в кВ
        RhiSlider.Value = Math.Round(systemData.HighResistanceLimit, 0); // Округляем до целого числа
        RloSlider.Value = Math.Round(systemData.LowResistanceLimit, 0); // Округляем до целого числа
        TimeSlider.Value = systemData.TestTime;
        RefSlider.Value = Math.Round(systemData.Offset, 0); // Округляем до целого числа

        // Обновляем текстовые блоки с текущими значениями
        VoltageValueText.Text = $"Напряжение IR: {systemData.Voltage / 1000.0:F3} кВ";
        RhiValueText.Text = $"Высокий предел сопротивления IR: {systemData.HighResistanceLimit:F1} G";
        RloValueText.Text = $"Низкий предел сопротивления IR: {systemData.LowResistanceLimit:F1} G";
        TimeValueText.Text = $"Время теста IR: {systemData.TestTime:F1} сек";
        RefValueText.Text = $"Смещение IR: {systemData.Offset:F1} G";
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show($"Ошибка при загрузке конфигурации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void ReadConfigurationButton_Click(object sender, RoutedEventArgs e)
    {
      var systemData = await GPTPunchControl.ModelGPT.IrManger.Config.ReadConfigurationAsync();
      try
      {
        // Обновляем данные
        VoltageValueText.Text = $"Напряжение IR: {systemData.Voltage * 1000.0} В";
        RhiValueText.Text = $"Высокий предел сопротивления IR: {systemData.HighResistanceLimit:F1} G";
        RloValueText.Text = $"Низкий предел сопротивления IR: {systemData.LowResistanceLimit:F1} G";
        TimeValueText.Text = $"Время теста IR: {systemData.TestTime:F1} сек";
        RefValueText.Text = $"Смещение IR: {systemData.Offset:F1} G";

        // Обновляем дату и время последнего считывания
        LastReadTimeText.Text = $"Дата и время: {DateTime.Now}";
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show($"Ошибка при считывании конфигурации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void StartTestButton_Click(object sender, RoutedEventArgs e)
    {
      TestResultText.Text = $"Результат теста: ???";
      try
      {
        var systemData = await GPTPunchControl.ModelGPT.IrManger.Config.ReadConfigurationAsync();

        VoltageValueText.Text = $"Напряжение IR: {systemData.Voltage / 1000.0:F3} кВ";
        RhiValueText.Text = $"Высокий предел сопротивления IR: {systemData.HighResistanceLimit:F1} G";
        RloValueText.Text = $"Низкий предел сопротивления IR: {systemData.LowResistanceLimit:F1} G";
        TimeValueText.Text = $"Время теста IR: {systemData.TestTime:F1} сек";
        RefValueText.Text = $"Смещение IR: {systemData.Offset:F1} G";

        var answer = await GPTPunchControl.ModelGPT.IrManger.Measure.MeasureAsync();
        TestResultText.Text = $"Результат теста: {answer} ГОм";
      }
      catch (Exception ex)
      {
        TestResultText.Text = $"Результат теста: ОШИБКА!";
        MessageBoxCustom.Show($"Ошибка при запуске теста: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void Button_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (!connect)
      {
        var mode = await GPTPunchControl.ModelGPT.IrManger.Mode.SetModeAsync();
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
      // Сначала собираем значения с UI строго в UI-потоке
      double voltage = 0, rhi = 0, rlo = 0, time = 0, timeRamp = 0, refValue = 0;

      Dispatcher.Invoke(() =>
      {
        voltage = Math.Round(VoltageSlider.Value, 3);
        rhi = Math.Round(RhiSlider.Value, 3);
        rlo = Math.Round(RloSlider.Value, 3);
        time = Math.Round(TimeSlider.Value, 1);
        timeRamp = Math.Round(RampTimeSlider.Value, 1);
        refValue = Math.Round(RefSlider.Value, 3);
      });

      await GPTPunchControl.ModelGPT.IrManger.Voltage.SetVoltageAsync(voltage).ConfigureAwait(false);
      await GPTPunchControl.ModelGPT.IrManger.Time.SetTestTimeAsync(time).ConfigureAwait(false);
      await GPTPunchControl.ModelGPT.IrManger.ResistanceLimits.SetLowResistanceLimitAsync(rlo).ConfigureAwait(false);
      await GPTPunchControl.ModelGPT.IrManger.ResistanceLimits.SetHighResistanceLimitAsync(rhi).ConfigureAwait(false);
      await GPTPunchControl.ModelGPT.IrManger.Time.SetRampTimeAsync(timeRamp).ConfigureAwait(false);
      await GPTPunchControl.ModelGPT.IrManger.Offset.SetOffsetAsync(refValue).ConfigureAwait(false);
    }
  }
}