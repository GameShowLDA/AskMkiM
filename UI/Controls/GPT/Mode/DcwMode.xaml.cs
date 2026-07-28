using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.GPT.Mode
{
  /// <summary>
  /// Компонент для управления режимом DCW.
  /// При инициализации устанавливается режим DCW и загружается конфигурация устройства.
  /// </summary>
  public partial class DcwMode : UserControl, IGptModeControl
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
        var systemData = await GptUiOperation.GetDevice().DcwManger.Config.ReadConfigurationAsync();

        VoltageSlider.Value = systemData.Voltage * 1000.0;
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
        GptUiOperation.ReportError("загрузка конфигурации DCW", ex);
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
        var systemData = await GptUiOperation.GetDevice().DcwManger.Config.ReadConfigurationAsync();
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
        GptUiOperation.ReportError("чтение конфигурации DCW", ex);
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
        MeasurementRange measurementRange = new MeasurementRange(0, 0, 0);
        double result = (await GptUiOperation.GetDevice().DcwManger.Measure.MeasureAsync(ElectricalTestFunction.DielectricWithstandDC, measurementRange)).value;
        TestResultText.Text = $"Результат теста: {result:F3} мА";
      }
      catch (Exception ex)
      {
        TestResultText.Text = "Результат теста: ошибка оборудования";
        GptUiOperation.ReportError("запуск теста DCW", ex);
      }
    }

    private bool connect = false;

    /// <inheritdoc />
    public BreakdownTypeMode ModeType => BreakdownTypeMode.DCW;

    /// <inheritdoc />
    public bool IsModeActive => connect;

    private async void Button_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      if (!connect)
      {
        await ActivateModeAsync();
      }
      else
      {
        DeactivateMode();
      }
    }

    /// <inheritdoc />
    public async Task<bool> ActivateModeAsync()
    {
      try
      {
        var mode = await GptUiOperation.GetDevice().DcwManger.Mode.SetModeAsync();
        GptUiOperation.EnsureSuccess(mode, "включение режима DCW");
        PanelManagment.Visibility = Visibility.Visible;
        await LoadConfigurationAsync();
        connect = true;
        ConnectButton.Content = "Отключить режим DCW";
        return true;
      }
      catch (Exception ex)
      {
        GptUiOperation.ReportError("переключение режима DCW", ex);
        return false;
      }
    }

    /// <inheritdoc />
    public void DeactivateMode()
    {
      PanelManagment.Visibility = Visibility.Collapsed;
      ConnectButton.Content = "Включить режим DCW";
      connect = false;
    }

    private async void Button_PreviewMouseDown_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      try
      {
        double voltage = Math.Round(VoltageSlider.Value, 3);
        double chi = Math.Round(ChiSlider.Value, 3);
        double clo = Math.Round(CloSlider.Value, 3);
        double time = Math.Round(TimeSlider.Value, 1);
        double timeRamp = Math.Round(RampSlider.Value, 1);
        double refValue = Math.Round(RefSlider.Value, 3);
        double arcCurrent = Math.Round(ArcCurrentSlider.Value, 3);

        var mode = GptUiOperation.GetDevice().DcwManger;
        GptUiOperation.EnsureSuccess(await mode.Voltage.SetVoltageAsync(voltage), "напряжение DCW");
        GptUiOperation.EnsureSuccess(await mode.Time.SetTestTimeAsync(time), "время теста DCW");
        GptUiOperation.EnsureSuccess(await mode.Time.SetRampTimeAsync(timeRamp), "время нарастания DCW");
        GptUiOperation.EnsureSuccess(await mode.Offset.SetOffsetAsync(refValue), "смещение DCW");
        GptUiOperation.EnsureSuccess(await mode.ArcCurrent.SetArcCurrentAsync(arcCurrent), "ток дуги DCW");
        GptUiOperation.EnsureSuccess(await mode.CurrentLimits.SetLowCurrentLimitAsync(clo), "нижний предел тока DCW");
        GptUiOperation.EnsureSuccess(await mode.CurrentLimits.SetHighCurrentLimitAsync(chi), "верхний предел тока DCW");
        Ask.LogLib.LoggerUtility.LogInformation("GPT — параметры DCW сохранены.", isDeviceLog: true);
      }
      catch (Exception ex)
      {
        GptUiOperation.ReportError("сохранение параметров DCW", ex);
      }
    }
  }
}
