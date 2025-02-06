using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.GPT.Mode
{
  /// <summary>
  /// Логика взаимодействия для DcwMode.xaml
  /// </summary>
  public partial class DcwMode : UserControl
  {
    public DcwMode()
    {
      InitializeComponent();
      Core.GptLibrary.DcwMode.SetModeAsync(GPTPunchControl.ModelGPT).ConfigureAwait(true);
      LoadConfigurationAsync().ConfigureAwait(true); // Загружаем конфигурацию при инициализации
    }

    /// <summary>
    /// Метод для загрузки конфигурации и заполнения элементов управления.
    /// </summary>
    private async Task LoadConfigurationAsync()
    {
      try
      {
        var systemData = await Core.GptLibrary.DcwMode.ReadConfigurationAsync(GPTPunchControl.ModelGPT);

        // Обновляем элементы управления
        VoltageSlider.Value = systemData.Voltage;
        ChiSlider.Value = systemData.HighCurrentLimit;
        CloSlider.Value = systemData.LowCurrentLimit;
        TimeSlider.Value = systemData.TestTime;
        RefSlider.Value = systemData.Offset;
        ArcCurrentSlider.Value = systemData.ArcCurrent;

        // Обновляем текстовые блоки
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
        MessageBox.Show($"Ошибка при считывании конфигурации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void VoltageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double voltage = Math.Round(VoltageSlider.Value, 3);
      await Core.GptLibrary.DcwMode.SetVoltageAsync(GPTPunchControl.ModelGPT, voltage);
    }

    private async void ChiSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double chi = Math.Round(ChiSlider.Value, 3);
      await Core.GptLibrary.DcwMode.SetHighCurrentLimitAsync(GPTPunchControl.ModelGPT, chi);
    }

    private async void CloSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double clo = Math.Round(CloSlider.Value, 3);
      await Core.GptLibrary.DcwMode.SetLowCurrentLimitAsync(GPTPunchControl.ModelGPT, clo);
    }

    private async void TimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double time = Math.Round(TimeSlider.Value, 1);
      await Core.GptLibrary.DcwMode.SetTestTimeAsync(GPTPunchControl.ModelGPT, time);
    }

    private async void RefSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double refValue = Math.Round(RefSlider.Value, 3);
      await Core.GptLibrary.DcwMode.SetOffsetAsync(GPTPunchControl.ModelGPT, refValue);
    }

    private async void ArcCurrentSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      if (ArcCurrentSlider != null)
      {
        double arcCurrent = Math.Round(ArcCurrentSlider.Value, 3);
        await Core.GptLibrary.AcwMode.SetArcCurrentAsync(GPTPunchControl.ModelGPT, arcCurrent);
      }
    }

    private async void ReadConfigurationButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        var systemData = await Core.GptLibrary.DcwMode.ReadConfigurationAsync(GPTPunchControl.ModelGPT);
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
        MessageBox.Show($"Ошибка при считывании конфигурации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
    private async void StartTestButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        double result = await Core.GptLibrary.DcwMode.MeasureCurrentAsync(GPTPunchControl.ModelGPT);
        TestResultText.Text = $"Результат теста: {result:F3} мА";
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Ошибка при запуске теста: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
  }
}
