using System.Windows;
using System.Windows.Controls;
using Message;

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
      GPTPunchControl.ModelGPT.IrManger.SetModeAsync().ConfigureAwait(true);
      LoadConfigurationAsync().ConfigureAwait(true); // Загружаем конфигурацию при инициализации
    }

    /// <summary>
    /// Метод для загрузки конфигурации и заполнения элементов управления.
    /// </summary>
    private async Task LoadConfigurationAsync()
    {
      try
      {
        var systemData = await GPTPunchControl.ModelGPT.IrManger.ReadConfigurationAsync();

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
      var systemData = await GPTPunchControl.ModelGPT.IrManger.ReadConfigurationAsync();
      try
      {
        // Обновляем данные
        VoltageValueText.Text = $"Напряжение IR: {systemData.Voltage / 1000.0:F3} кВ";
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

    /// <summary>
    /// Обработчик изменения напряжения IR.
    /// </summary>
    private async void VoltageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double voltage = VoltageSlider.Value * 1000;
      await GPTPunchControl.ModelGPT.IrManger.SetVoltageAsync(voltage);
    }

    /// <summary>
    /// Обработчик изменения времени теста IR.
    /// </summary>
    private async void TimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double time = TimeSlider.Value;
      await GPTPunchControl.ModelGPT.IrManger.SetTestTimeAsync(time);
    }

    private async void RhiSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double value = Math.Round(RhiSlider.Value, 0); // Округляем до целого числа
      await GPTPunchControl.ModelGPT.IrManger.SetHighResistanceLimitAsync(value);
    }

    private async void RloSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double value = Math.Round(RloSlider.Value, 0); // Округляем до целого числа
      await GPTPunchControl.ModelGPT.IrManger.SetLowResistanceLimitAsync(value);
    }

    private async void RefSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double value = Math.Round(RefSlider.Value, 0); // Округляем до целого числа
      await GPTPunchControl.ModelGPT.IrManger.SetOffsetAsync(value);
    }

    private async void StartTestButton_Click(object sender, RoutedEventArgs e)
    {
      TestResultText.Text = $"Результат теста: ???";
      try
      {
        var systemData = await GPTPunchControl.ModelGPT.IrManger.ReadConfigurationAsync();

        VoltageValueText.Text = $"Напряжение IR: {systemData.Voltage / 1000.0:F3} кВ";
        RhiValueText.Text = $"Высокий предел сопротивления IR: {systemData.HighResistanceLimit:F1} G";
        RloValueText.Text = $"Низкий предел сопротивления IR: {systemData.LowResistanceLimit:F1} G";
        TimeValueText.Text = $"Время теста IR: {systemData.TestTime:F1} сек";
        RefValueText.Text = $"Смещение IR: {systemData.Offset:F1} G";

        var answer = await GPTPunchControl.ModelGPT.IrManger.MeasureResistanceAsync();
        TestResultText.Text = $"Результат теста: {answer} ГОм";
      }
      catch (Exception ex)
      {
        TestResultText.Text = $"Результат теста: ОШИБКА!";
        MessageBoxCustom.Show($"Ошибка при запуске теста: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
  }
}