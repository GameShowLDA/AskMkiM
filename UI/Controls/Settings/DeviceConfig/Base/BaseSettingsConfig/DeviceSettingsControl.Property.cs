using Ask.Core.Shared.Interfaces.DeviceInterfaces;
using System.Globalization;
using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;

namespace UI.Controls.Settings.DeviceConfig.Base.BaseSettingsConfig
{
  /// <summary>
  /// Частичный класс управления настройками устройства.
  /// </summary>
  public partial class DeviceSettingsControl
  {
    private TextBox? _acwPpuDividerCoefficientPercentTextBox;
    private TextBox? _dcwPpuDividerCoefficientPercentTextBox;

    /// <summary>
    /// Экземпляр головного устройства.
    /// </summary>
    private IHeadUnit _headUnit;

    /// <summary>
    /// Словарь соответствия моделей устройств и их типов.
    /// </summary>
    public Dictionary<string, Type> DeviceModelMap { get; set; }

    /// <summary>
    /// Событие сохранения настроек устройства.
    /// </summary>
    public event EventHandler SaveEvent;

    /// <summary>
    /// Событие запроса на закрытие окна настроек.
    /// </summary>
    public event EventHandler RequestClose;

    /// <summary>
    /// Получает или задает название устройства в заголовке окна.
    /// </summary>
    public string NameDevice
    {
      get => Header.Text;
      set => Header.Text = $"Настройка устройства: \"{value}\"";
    }

    /// <summary>
    /// Получает номер шасси.
    /// </summary>
    public int NumberChassis => _headUnit?.Number ?? -1;

    /// <summary>
    /// Свойство для добавления дополнительных настроек из других элементов управления.
    /// </summary>
    public UIElement AdditionalSettings
    {
      get => (UIElement)GetValue(AdditionalSettingsProperty);
      set => SetValue(AdditionalSettingsProperty, value);
    }

    /// <summary>
    /// Свойство зависимости для хранения дополнительных настроек.
    /// </summary>
    public static readonly DependencyProperty AdditionalSettingsProperty =
        DependencyProperty.Register(
            "AdditionalSettings",
            typeof(UIElement),
            typeof(DeviceSettingsControl),
            new PropertyMetadata(null, OnAdditionalSettingsChanged));

    /// <summary>
    /// Получает значение первой части IP-адреса.
    /// </summary>
    public int IpPart1Value => int.TryParse(IpPart1.Text, out int ipValue) ? ipValue : -1;

    /// <summary>
    /// Получает значение второй части IP-адреса.
    /// </summary>
    public int IpPart2Value => int.TryParse(IpPart2.Text, out int ipValue) ? ipValue : -1;

    /// <summary>
    /// Получает значение третьей части IP-адреса.
    /// </summary>
    public int IpPart3Value => int.TryParse(IpPart3.Text, out int ipValue) ? ipValue : -1;

    /// <summary>
    /// Получает значение четвертой части IP-адреса.
    /// </summary>
    public int IpPart4Value => int.TryParse(IpPart4.Text, out int ipValue) ? ipValue : -1;

    /// <summary>
    /// Получает значение скорости передачи данных (BaudRate).
    /// </summary>
    public int BaudRateValue =>
        BaudRateSelectionBox?.SelectedItem is ComboBoxItem selectedItem &&
        int.TryParse(selectedItem.Content?.ToString(), out int baudRate) ? baudRate : -1;

    /// <summary>
    /// Получает значение количества бит данных.
    /// </summary>
    public int DataBitsValue =>
        DataBitsSelectionBox?.SelectedItem is ComboBoxItem selectedItem &&
        int.TryParse(selectedItem.Content?.ToString(), out int dataBits) ? dataBits : -1;

    /// <summary>
    /// Получает выбранное значение четности порта.
    /// </summary>
    public Parity ParityValue =>
        BaseHandler<IDevice>.ValuePairs.TryGetValue(
            (ParitySelectionBox.SelectedItem as ComboBoxItem)?.Content?.ToString(),
            out Parity parity)
            ? parity
            : Parity.None;

    /// <summary>
    /// Получает выбранное количество стоп-бит.
    /// </summary>
    public StopBits StopBitsValue =>
        BaseHandler<IDevice>.StopBitsPairs.TryGetValue(
            (StopBitsSelectionBox.SelectedItem as ComboBoxItem)?.Content?.ToString(),
            out StopBits stopBits)
            ? stopBits
            : StopBits.One;

    /// <summary>
    /// Получает название COM-порта.
    /// </summary>
    public string PortName => COMPortSelectionBox.Text;

    /// <summary>
    /// Gets USB connection details selected or resolved by UI.
    /// </summary>
    public string UsbConnectionDetails => _usbConnectionDetails;

    /// <summary>
    /// Получает номер устройства.
    /// </summary>
    public int NumberDevice => int.TryParse(DeviceNumberTextBox.Text, out int number) ? number : -1;

    public double GetResistance()
    {
      if (ResistanceTextBox == null)
        return 0;

      var text = ResistanceTextBox.Text?.Trim();

      if (string.IsNullOrEmpty(text))
        return 0;

      text = text.Replace(',', '.');

      if (text.EndsWith("."))
      {
        text += "0";
        ResistanceTextBox.Text = text;
      }

      return double.Parse(
        text,
        CultureInfo.InvariantCulture
      );
    }

    public double GetCapacitance()
    {
      if (CapacitanceTextBox == null)
        return 0;

      var text = CapacitanceTextBox.Text?.Trim();

      if (string.IsNullOrEmpty(text))
        return 0;

      text = text.Replace(',', '.');

      if (text.EndsWith("."))
      {
        text += "0";
        CapacitanceTextBox.Text = text;
      }

      return double.Parse(
        text,
        CultureInfo.InvariantCulture
      );
    }

    public void SetPpuDividerCoefficientPercent(double acwValue, double dcwValue)
    {
      _acwPpuDividerCoefficientPercentTextBox ??= new TextBox();
      _dcwPpuDividerCoefficientPercentTextBox ??= new TextBox();
      SetPpuDividerCoefficientText(_acwPpuDividerCoefficientPercentTextBox, acwValue);
      SetPpuDividerCoefficientText(_dcwPpuDividerCoefficientPercentTextBox, dcwValue);
    }

    public double GetAcwPpuDividerCoefficientPercent()
    {
      return GetPpuDividerCoefficientPercent(_acwPpuDividerCoefficientPercentTextBox);
    }

    public double GetDcwPpuDividerCoefficientPercent()
    {
      return GetPpuDividerCoefficientPercent(_dcwPpuDividerCoefficientPercentTextBox);
    }

    private static void SetPpuDividerCoefficientText(TextBox textBox, double value)
    {
      textBox.Text = (value > 0 ? value : 100d).ToString(CultureInfo.InvariantCulture);
    }

    private static double GetPpuDividerCoefficientPercent(TextBox? textBox)
    {
      if (textBox == null)
        return 100d;

      var text = textBox.Text?.Trim();

      if (string.IsNullOrEmpty(text))
        return 100d;

      text = text.Replace(',', '.');

      if (text.EndsWith("."))
      {
        text += "0";
        textBox.Text = text;
      }

      return double.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)
        ? value
        : -1d;
    }
  }
}
