using System.IO.Ports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mode.Settings.DeviceConfig.Base.BaseSettings
{
  /// <summary>
  /// Базовый класс для управления элементами конфигурации устройства.
  /// Предоставляет доступ к основным элементам управления, таким как
  /// выпадающие списки выбора модели устройства, типа подключения,
  /// параметров COM-порта и текстовые поля для ввода IP-адреса и идентификаторов USB.
  /// </summary>
  partial class BaseSettingsControl
  {
    public Dictionary<string, Type> DeviceModelMap = new Dictionary<string, Type>();

    #region Элменты упраления.

    /// <summary>
    /// Выпадающий список для выбора модели устройства.
    /// </summary>
    public ComboBox DeviceModelComboBoxControl => DeviceModelSelectionBox;

    /// <summary>
    /// Текстовое поле для ввода номера устройства.
    /// </summary>
    public TextBox DeviceNumberTextBoxControl => DeviceNumberInput;

    /// <summary>
    /// Выпадающий список для выбора типа подключения.
    /// </summary>
    public ComboBox ConnectionTypeComboBoxControl => ConnectionTypeSelectionBox;

    /// <summary>
    /// Текстовое поле для ввода третьего октета IP-адреса.
    /// </summary>
    public TextBox IpPart3TextBoxControl => IPAddressPart3Input;

    /// <summary>
    /// Текстовое поле для ввода четвертого октета IP-адреса.
    /// </summary>
    public TextBox IpPart4TextBoxControl => IPAddressPart4Input;

    /// <summary>
    /// Текстовое поле для ввода идентификатора VID (Vendor ID) USB-устройства.
    /// </summary>
    public TextBox VidDataControl => VIDInput;

    /// <summary>
    /// Текстовое поле для ввода идентификатора PID (Product ID) USB-устройства.
    /// </summary>
    public TextBox PidDataControl => PIDInput;

    /// <summary>
    /// Выпадающий список для выбора COM-порта.
    /// </summary>
    public ComboBox ComPortComboBoxControl => COMPortSelectionBox;

    /// <summary>
    /// Выпадающий список для выбора скорости передачи данных (Baud Rate).
    /// </summary>
    public ComboBox BaudRateComboBoxControl => BaudRateSelectionBox;

    /// <summary>
    /// Выпадающий список для выбора количества бит данных (Data Bits).
    /// </summary>
    public ComboBox DataBitsComboBoxControl => DataBitsSelectionBox;

    /// <summary>
    /// Выпадающий список для выбора типа управления потоком (Flow Control).
    /// </summary>
    public ComboBox FlowControlComboBoxControl => FlowControlSelectionBox;

    /// <summary>
    /// Выпадающий список для выбора количества стоп-бит (Stop Bits).
    /// </summary>
    public ComboBox StopBitsComboBoxControl => StopBitsSelectionBox;

    /// <summary>
    /// Выпадающий список для выбора типа контроля четности (Parity).
    /// </summary>
    public ComboBox ParityComboBoxControl => ParitySelectionBox;

    /// <summary>
    /// Выпадающий список для тестеров и менеджера шасси.
    /// </summary>
    public ComboBox ChassisModelsComboBox => ChassisModelComboBox;

    #endregion

    #region Свойства управления доступностью элементов.

    /// <summary>
    /// Определяет, доступен ли выбор номера шасси.
    /// При активации скрывает выбор типа устройства и наоборот.
    /// </summary>
    public bool IsChassisNumberEnabled
    {
      get => ChassisNumberBorder.IsEnabled;
      set
      {
        ChassisNumberBorder.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        DeviceTypeBorder.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
      }
    }

    /// <summary>
    /// Определяет, доступен ли выбор номера шасси.
    /// При активации скрывает выбор типа устройства и наоборот.
    /// </summary>
    public bool IsRackNumberEnabled
    {
      get => RacksNumberBorder.IsEnabled;
      set => RacksNumberBorder.IsEnabled = value;
    }

    /// <summary>
    /// Определяет, доступен ли ввод третьей части IP-адреса.
    /// Также изменяет стиль границы текстового поля.
    /// </summary>
    public bool IsIpPart3Enabled
    {
      get => IPAddressPart3Input.IsEnabled;
      set
      {
        IPAddressPart3Input.IsReadOnly = !value;
        IPAddressPart3Input.BorderBrush = value
            ? (Brush)FindResource("ActiveBorderSolidColorBrush")
            : Brushes.White;
      }
    }

    /// <summary>
    /// Определяет, доступен ли ввод четвертой части IP-адреса.
    /// Также изменяет стиль границы текстового поля.
    /// </summary>
    public bool IsIpPart4Enabled
    {
      get => IPAddressPart4Input.IsEnabled;
      set
      {
        IPAddressPart4Input.IsReadOnly = !value;
        IPAddressPart4Input.BorderBrush = value
            ? (Brush)FindResource("ActiveBorderSolidColorBrush")
            : Brushes.White;
      }
    }

    #endregion

    #region Свойства содержимого полей IP-адреса.

    /// <summary>
    /// Получает или задает значение третьей части IP-адреса.
    /// </summary>
    public int IpPart3Content
    {
      get => int.TryParse(IPAddressPart3Input.Text, out int result) ? result : 0;
      set => IPAddressPart3Input.Text = value.ToString();
    }

    /// <summary>
    /// Получает или задает значение четвертой части IP-адреса.
    /// </summary>
    public int IpPart4Content
    {
      get => int.TryParse(IPAddressPart4Input.Text, out int result) ? result : 0;
      set => IPAddressPart4Input.Text = value.ToString();
    }

    #endregion

    #region Параметры выбора.

    int BaudRate { get; set; }
    int DataBits { get; set; }
    string FlowControlMode { get; set; }
    StopBits StopBitsMode { get; set; }
    Parity ParityMode { get; set; }

    #endregion
  }
}
