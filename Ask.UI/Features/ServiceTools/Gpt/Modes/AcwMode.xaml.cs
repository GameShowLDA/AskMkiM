using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Features.ServiceTools.Gpt.Modes
{
  /// <summary>
  /// Компонент для работы с режимом ACW.
  /// При инициализации устанавливает режим ACW и загружает конфигурацию устройства.
  /// </summary>
  public partial class AcwMode : UserControl, IGptModeControl
  {
    private readonly GptDeviceContext deviceContext;

    /// <summary>
    /// Инициализирует новый экземпляр класса <see cref="AcwMode"/>.
    /// При инициализации устанавливается режим ACW и запускается загрузка конфигурации.
    /// </summary>
    /// <param name="deviceContext">Контекст пробойной установки текущей вкладки.</param>
    internal AcwMode(GptDeviceContext deviceContext)
    {
      this.deviceContext = deviceContext;
      InitializeComponent();
    }

    private bool connect = false;

    /// <inheritdoc />
    public BreakdownTypeMode ModeType => BreakdownTypeMode.ACW;

    /// <inheritdoc />
    public bool IsModeActive => connect;

    /// <summary>
    /// Асинхронно загружает конфигурацию устройства и обновляет элементы управления.
    /// </summary>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    private async Task LoadConfigurationAsync()
    {
      try
      {
        var systemData = await GptUiOperation.GetDevice(deviceContext).AcwManger.Config.ReadConfigurationAsync();

        VoltageSlider.Value = systemData.Voltage * 1000.0;
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
        GptUiOperation.ReportError("загрузка конфигурации ACW", ex);
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
        var systemData = await GptUiOperation.GetDevice(deviceContext).AcwManger.Config.ReadConfigurationAsync();
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
        GptUiOperation.ReportError("чтение конфигурации ACW", ex);
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
        MeasurementRange measurementRange = new MeasurementRange(0, 0, 0);
        double result = (await GptUiOperation.GetDevice(deviceContext).AcwManger.Measure.MeasureAsync(ElectricalTestFunction.DielectricWithstandAC, measurementRange)).Value;

        TestResultText.Text = $"Результат теста: {result:F3} мА";
      }
      catch (Exception ex)
      {
        TestResultText.Text = "Результат теста: ошибка оборудования";
        GptUiOperation.ReportError("запуск теста ACW", ex);
      }
    }

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
        var mode = await GptUiOperation.GetDevice(deviceContext).AcwManger.Mode.SetModeAsync();
        GptUiOperation.EnsureSuccess(mode, "включение режима ACW");
        PanelManagment.Visibility = Visibility.Visible;
        await LoadConfigurationAsync();
        connect = true;
        SetConnectButtonState(isEnabled: true);
        return true;
      }
      catch (Exception ex)
      {
        GptUiOperation.ReportError("переключение режима ACW", ex);
        return false;
      }
    }

    /// <inheritdoc />
    public void DeactivateMode()
    {
      PanelManagment.Visibility = Visibility.Collapsed;
      SetConnectButtonState(isEnabled: false);
      connect = false;
    }

    private void SetConnectButtonState(bool isEnabled)
    {
      ConnectButton.Content = isEnabled ? "Выключить" : "Включить";
      ConnectButton.SetResourceReference(
        BackgroundProperty,
        isEnabled ? "RedColorSolidColorBrush" : "GreenColorSolidColorBrush");
      ConnectButton.SetResourceReference(
        BorderBrushProperty,
        isEnabled ? "RedColorSolidColorBrush" : "GreenColorSolidColorBrush");
    }

    private async void Button_PreviewMouseDown_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
      try
      {
        double voltage = Math.Round(VoltageSlider.Value, 3);
        double chi = Math.Round(ChiSlider.Value, 3);
        double clo = Math.Round(CloSlider.Value, 3);
        double time = Math.Round(TimeSlider.Value, 1);
        double timeRamp = Math.Round(RampTimeSlider.Value, 1);
        double refValue = Math.Round(RefSlider.Value, 3);
        double arcCurrent = Math.Round(ArcCurrentSlider.Value, 3);
        int frequency = 50;

        if (FrequencyComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
          string frequencyText = selectedItem.Content.ToString() ?? string.Empty;
          if (double.TryParse(frequencyText.Replace("Гц", "").Trim(), out double freq))
          {
            frequency = (int)freq;
          }
        }

        var mode = GptUiOperation.GetDevice(deviceContext).AcwManger;
        GptUiOperation.EnsureSuccess(await mode.Voltage.SetVoltageAsync(voltage), "напряжение ACW");
        GptUiOperation.EnsureSuccess(await mode.Time.SetTestTimeAsync(time), "время теста ACW");
        GptUiOperation.EnsureSuccess(await mode.Time.SetRampTimeAsync(timeRamp), "время нарастания ACW");
        GptUiOperation.EnsureSuccess(await mode.FrequencyConfigurable.SetFrequencyAsync(frequency), "частота ACW");
        GptUiOperation.EnsureSuccess(await mode.CurrentLimits.SetHighCurrentLimitAsync(chi), "верхний предел тока ACW");
        GptUiOperation.EnsureSuccess(await mode.CurrentLimits.SetLowCurrentLimitAsync(clo), "нижний предел тока ACW");
        GptUiOperation.EnsureSuccess(await mode.Offset.SetOffsetAsync(refValue), "смещение ACW");
        GptUiOperation.EnsureSuccess(await mode.ArcCurrent.SetArcCurrentAsync(arcCurrent), "ток дуги ACW");
        Ask.LogLib.LoggerUtility.LogInformation("GPT — параметры ACW сохранены.", isDeviceLog: true);
      }
      catch (Exception ex)
      {
        GptUiOperation.ReportError("сохранение параметров ACW", ex);
      }
    }
  }
}
