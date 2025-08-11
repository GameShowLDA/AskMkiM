using System.Windows;
using System.Windows.Controls;
using Message;

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
      GPTPunchControl.ModelGPT.DcwManger.SetModeAsync().ConfigureAwait(true);
      LoadConfigurationAsync().ConfigureAwait(true); // Загружаем конфигурацию при инициализации
    }

    /// <summary>
    /// Асинхронно загружает конфигурацию устройства и обновляет элементы управления.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию загрузки конфигурации.</returns>
    private async Task LoadConfigurationAsync()
    {
      try
      {
        var systemData = await GPTPunchControl.ModelGPT.DcwManger.ReadConfigurationAsync();

        VoltageSlider.Value = systemData.Voltage;
        ChiSlider.Value = systemData.HighCurrentLimit;
        CloSlider.Value = systemData.LowCurrentLimit;
        TimeSlider.Value = systemData.TestTime;
        RefSlider.Value = systemData.Offset;
        ArcCurrentSlider.Value = systemData.ArcCurrent;

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
    /// Обрабатывает изменение значения слайдера для напряжения.
    /// Округляет значение и отправляет новое значение напряжения на устройство.
    /// </summary>
    /// <param name="sender">Источник события (слайдер).</param>
    /// <param name="e">Данные события изменения значения слайдера.</param>
    private async void VoltageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double voltage = Math.Round(VoltageSlider.Value, 3);
      await GPTPunchControl.ModelGPT.DcwManger.SetVoltageAsync(voltage);
    }

    /// <summary>
    /// Обрабатывает изменение значения слайдера для высокого предела тока.
    /// Округляет значение и отправляет новое значение высокого предела тока на устройство.
    /// </summary>
    /// <param name="sender">Источник события (слайдер).</param>
    /// <param name="e">Данные события изменения значения слайдера.</param>
    private async void ChiSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double chi = Math.Round(ChiSlider.Value, 3);
      await GPTPunchControl.ModelGPT.DcwManger.SetHighCurrentLimitAsync(chi);
    }

    /// <summary>
    /// Обрабатывает изменение значения слайдера для низкого предела тока.
    /// Округляет значение и отправляет новое значение низкого предела тока на устройство.
    /// </summary>
    /// <param name="sender">Источник события (слайдер).</param>
    /// <param name="e">Данные события изменения значения слайдера.</param>
    private async void CloSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double clo = Math.Round(CloSlider.Value, 3);
      await GPTPunchControl.ModelGPT.DcwManger.SetLowCurrentLimitAsync(clo);
    }

    /// <summary>
    /// Обрабатывает изменение значения слайдера для времени теста.
    /// Округляет значение и отправляет новое значение времени теста на устройство.
    /// </summary>
    /// <param name="sender">Источник события (слайдер).</param>
    /// <param name="e">Данные события изменения значения слайдера.</param>
    private async void TimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double time = Math.Round(TimeSlider.Value, 1);
      await GPTPunchControl.ModelGPT.DcwManger.SetTestTimeAsync(time);
    }

    /// <summary>
    /// Обрабатывает изменение значения слайдера для смещения.
    /// Округляет значение и отправляет новое значение смещения на устройство.
    /// </summary>
    /// <param name="sender">Источник события (слайдер).</param>
    /// <param name="e">Данные события изменения значения слайдера.</param>
    private async void RefSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      double refValue = Math.Round(RefSlider.Value, 3);
      await GPTPunchControl.ModelGPT.DcwManger.SetOffsetAsync(null, refValue);
    }

    /// <summary>
    /// Обрабатывает изменение значения слайдера для тока дуги.
    /// Округляет значение и отправляет новое значение тока дуги на устройство.
    /// </summary>
    /// <param name="sender">Источник события (слайдер).</param>
    /// <param name="e">Данные события изменения значения слайдера.</param>
    private async void ArcCurrentSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      if (ArcCurrentSlider != null)
      {
        double arcCurrent = Math.Round(ArcCurrentSlider.Value, 3);
        await GPTPunchControl.ModelGPT.DcwManger.SetArcCurrentAsync(arcCurrent);
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
        var systemData = await GPTPunchControl.ModelGPT.DcwManger.ReadConfigurationAsync();
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
        double result = await GPTPunchControl.ModelGPT.DcwManger.MeasureCurrentAsync();
        TestResultText.Text = $"Результат теста: {result:F3} мА";
      }
      catch (Exception ex)
      {
        MessageBoxCustom.Show($"Ошибка при запуске теста: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
  }
}
