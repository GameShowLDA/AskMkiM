using Ask.Core.Shared.DTO.Devices.Measurements;
using Ask.Core.Shared.Metadata.Enums.DeviceEnums;
using System.Windows;
using System.Windows.Controls;

namespace Ask.UI.Features.ServiceTools.Gpt.Modes
{
  public partial class IrMode : UserControl, IGptModeControl
  {
    private readonly GptDeviceContext deviceContext;

    /// <summary>
    /// Компонент для управления режимом Ir.
    /// При инициализации устанавливается режим Ir и загружается конфигурация устройства.
    /// </summary>
    /// <param name="deviceContext">Контекст пробойной установки текущей вкладки.</param>
    internal IrMode(GptDeviceContext deviceContext)
    {
      this.deviceContext = deviceContext;
      InitializeComponent();
    }

    private bool connect = false;

    /// <inheritdoc />
    public BreakdownTypeMode ModeType => BreakdownTypeMode.IR;

    /// <inheritdoc />
    public bool IsModeActive => connect;

    /// <summary>
    /// Метод для загрузки конфигурации и заполнения элементов управления.
    /// </summary>
    private async Task LoadConfigurationAsync()
    {
      try
      {
        var systemData = await GptUiOperation.GetDevice(deviceContext).IrManger.Config.ReadConfigurationAsync();

        VoltageSlider.Value = systemData.Voltage * 1000.0;
        RhiSlider.Value = Math.Round(systemData.HighResistanceLimit, 0);
        RloSlider.Value = Math.Round(systemData.LowResistanceLimit, 0);
        TimeSlider.Value = systemData.TestTime;
        RampTimeSlider.Value = systemData.RampTime;
        RefSlider.Value = Math.Round(systemData.Offset, 0);

        VoltageValueText.Text = $"Напряжение IR: {systemData.Voltage:F3} кВ";
        RhiValueText.Text = $"Высокий предел сопротивления IR: {systemData.HighResistanceLimit:F1} G";
        RloValueText.Text = $"Низкий предел сопротивления IR: {systemData.LowResistanceLimit:F1} G";
        TimeValueText.Text = $"Время теста IR: {systemData.TestTime:F1} сек";
        RefValueText.Text = $"Смещение IR: {systemData.Offset:F1} G";
      }
      catch (Exception ex)
      {
        GptUiOperation.ReportError("загрузка конфигурации IR", ex);
      }
    }

    private async void ReadConfigurationButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        var systemData = await GptUiOperation.GetDevice(deviceContext).IrManger.Config.ReadConfigurationAsync();
        VoltageValueText.Text = $"Напряжение IR: {systemData.Voltage * 1000.0} В";
        RhiValueText.Text = $"Высокий предел сопротивления IR: {systemData.HighResistanceLimit:F1} G";
        RloValueText.Text = $"Низкий предел сопротивления IR: {systemData.LowResistanceLimit:F1} G";
        TimeValueText.Text = $"Время теста IR: {systemData.TestTime:F1} сек";
        RefValueText.Text = $"Смещение IR: {systemData.Offset:F1} G";

        LastReadTimeText.Text = $"Дата и время: {DateTime.Now}";
      }
      catch (Exception ex)
      {
        GptUiOperation.ReportError("чтение конфигурации IR", ex);
      }
    }

    private async void StartTestButton_Click(object sender, RoutedEventArgs e)
    {
      TestResultText.Text = $"Результат теста: ???";
      try
      {
        var systemData = await GptUiOperation.GetDevice(deviceContext).IrManger.Config.ReadConfigurationAsync();

        VoltageValueText.Text = $"Напряжение IR: {systemData.Voltage:F3} кВ";
        RhiValueText.Text = $"Высокий предел сопротивления IR: {systemData.HighResistanceLimit:F1} G";
        RloValueText.Text = $"Низкий предел сопротивления IR: {systemData.LowResistanceLimit:F1} G";
        TimeValueText.Text = $"Время теста IR: {systemData.TestTime:F1} сек";
        RefValueText.Text = $"Смещение IR: {systemData.Offset:F1} G";

        MeasurementRange measurementRange = new MeasurementRange(0, 0, 0);
        var answer = await GptUiOperation.GetDevice(deviceContext).IrManger.Measure.MeasureAsync(ElectricalTestFunction.InsulationResistance, measurementRange);
        TestResultText.Text = $"Результат теста: {answer.Value:F3} ГОм";
      }
      catch (Exception ex)
      {
        TestResultText.Text = "Результат теста: ошибка оборудования";
        GptUiOperation.ReportError("запуск теста IR", ex);
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
        var mode = await GptUiOperation.GetDevice(deviceContext).IrManger.Mode.SetModeAsync();
        GptUiOperation.EnsureSuccess(mode, "включение режима IR");
        PanelManagment.Visibility = Visibility.Visible;
        await LoadConfigurationAsync();
        connect = true;
        SetConnectButtonState(isEnabled: true);
        return true;
      }
      catch (Exception ex)
      {
        GptUiOperation.ReportError("переключение режима IR", ex);
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
        double rhi = Math.Round(RhiSlider.Value, 3);
        double rlo = Math.Round(RloSlider.Value, 3);
        double time = Math.Round(TimeSlider.Value, 1);
        double timeRamp = Math.Round(RampTimeSlider.Value, 1);
        double refValue = Math.Round(RefSlider.Value, 3);

        var mode = GptUiOperation.GetDevice(deviceContext).IrManger;
        GptUiOperation.EnsureSuccess(await mode.Voltage.SetVoltageAsync(voltage), "напряжение IR");
        GptUiOperation.EnsureSuccess(await mode.Time.SetTestTimeAsync(time), "время теста IR");
        GptUiOperation.EnsureSuccess(await mode.ResistanceLimits.SetLowResistanceLimitAsync(rlo), "нижний предел сопротивления IR");
        GptUiOperation.EnsureSuccess(await mode.ResistanceLimits.SetHighResistanceLimitAsync(rhi), "верхний предел сопротивления IR");
        GptUiOperation.EnsureSuccess(await mode.Time.SetRampTimeAsync(timeRamp), "время нарастания IR");
        GptUiOperation.EnsureSuccess(await mode.Offset.SetOffsetAsync(refValue), "смещение IR");
        Ask.LogLib.LoggerUtility.LogInformation("GPT — параметры IR сохранены.", isDeviceLog: true);
      }
      catch (Exception ex)
      {
        GptUiOperation.ReportError("сохранение параметров IR", ex);
      }
    }
  }
}
