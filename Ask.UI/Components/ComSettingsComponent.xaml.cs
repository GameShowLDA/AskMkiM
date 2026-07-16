using Ask.Device.Communication.Com.Configuration;
using System.IO.Ports;
using System.Management;
using System.Windows.Controls;

namespace Ask.UI.Components
{
  /// <summary>
  /// Компонент для просмотра и редактирования параметров последовательного COM-порта.
  /// </summary>
  public partial class ComSettingsComponent : UserControl
  {
    /// <summary>
    /// Инициализирует новый экземпляр компонента настроек COM-порта.
    /// </summary>
    public ComSettingsComponent()
    {
      InitializeComponent();
      Reset();
    }

    /// <summary>
    /// Получает имя выбранного COM-порта.
    /// </summary>
    public string PortName => COMPortSelectionBox.Text;

    /// <summary>
    /// Получает выбранную скорость передачи данных.
    /// </summary>
    public int BaudRate => GetIntegerValue(BaudRateSelectionBox);

    /// <summary>
    /// Получает выбранное количество бит данных.
    /// </summary>
    public int DataBits => GetIntegerValue(DataBitsSelectionBox);

    /// <summary>
    /// Получает выбранный режим контроля чётности.
    /// </summary>
    public Parity Parity => GetSelectedText(ParitySelectionBox) switch
    {
      "Чет" => Parity.Even,
      "Нечет" => Parity.Odd,
      "Маркер" => Parity.Mark,
      "Пробел" => Parity.Space,
      _ => Parity.None,
    };

    /// <summary>
    /// Получает выбранное количество стоп-битов.
    /// </summary>
    public StopBits StopBits => GetSelectedText(StopBitsSelectionBox) switch
    {
      "1.5" => StopBits.OnePointFive,
      "2" => StopBits.Two,
      _ => StopBits.One,
    };

    /// <summary>
    /// Загружает список доступных COM-портов и выбирает первый из них.
    /// </summary>
    public void LoadAvailablePorts()
    {
      string[] portNames = SerialPort.GetPortNames();
      COMPortSelectionBox.ItemsSource = portNames;
      COMPortSelectionBox.SelectedIndex = portNames.Length > 0 ? 0 : -1;
    }

    /// <summary>
    /// Применяет стандартные параметры COM-порта из выбранной модели устройства.
    /// </summary>
    /// <param name="deviceModel">Экземпляр модели устройства.</param>
    public void ApplyModelDefaults(object deviceModel)
    {
      ArgumentNullException.ThrowIfNull(deviceModel);
      Type modelType = deviceModel.GetType();
      SetValueFromProperty(modelType, deviceModel, "BaudRate", BaudRateSelectionBox);
      SetValueFromProperty(modelType, deviceModel, "StopBits", StopBitsSelectionBox);
      SetValueFromProperty(modelType, deviceModel, "DataBits", DataBitsSelectionBox);
      SetValueFromProperty(modelType, deviceModel, "Parity", ParitySelectionBox);
      SetValueFromProperty(modelType, deviceModel, "FlowControl", FlowControlSelectionBox);
    }

    /// <summary>
    /// Загружает сохранённые параметры последовательного порта в компонент.
    /// </summary>
    /// <param name="settings">Сохранённые параметры COM-порта.</param>
    public void Load(SerialPortCustom settings)
    {
      ArgumentNullException.ThrowIfNull(settings);
      LoadAvailablePorts();

      var ports = (COMPortSelectionBox.ItemsSource as IEnumerable<string>)?.ToList() ?? [];
      if (!ports.Contains(settings.PortName))
      {
        ports.Add(settings.PortName);
        COMPortSelectionBox.ItemsSource = ports;
      }

      COMPortSelectionBox.SelectedItem = settings.PortName;
      SetValue(BaudRateSelectionBox, settings.BaudRate.ToString());
      SetValue(DataBitsSelectionBox, settings.DataBits.ToString());
      SetValue(StopBitsSelectionBox, settings.StopBits switch
      {
        StopBits.OnePointFive => "1.5",
        StopBits.Two => "2",
        _ => "1",
      });
      SetValue(ParitySelectionBox, settings.Parity switch
      {
        Parity.Even => "Чет",
        Parity.Odd => "Нечет",
        Parity.Mark => "Маркер",
        Parity.Space => "Пробел",
        _ => "Нет",
      });
    }

    /// <summary>
    /// Создаёт объект параметров последовательного порта из выбранных значений.
    /// </summary>
    /// <returns>Параметры выбранного COM-порта.</returns>
    /// <exception cref="ArgumentException">Выбраны некорректные параметры COM-порта.</exception>
    public SerialPortCustom CreateSettings()
    {
      if (string.IsNullOrWhiteSpace(PortName) || BaudRate < 0 || DataBits < 0)
      {
        throw new ArgumentException("Укажите корректные параметры COM-порта.");
      }

      return new SerialPortCustom(PortName, BaudRate, Parity, DataBits, StopBits);
    }

    /// <summary>
    /// Очищает выбранный порт и возвращает остальные параметры к значениям по умолчанию.
    /// </summary>
    public void Reset()
    {
      COMPortSelectionBox.ItemsSource = null;
      COMPortSelectionBox.SelectedIndex = -1;
      BaudRateSelectionBox.SelectedIndex = 3;
      StopBitsSelectionBox.SelectedIndex = 0;
      DataBitsSelectionBox.SelectedIndex = 4;
      ParitySelectionBox.SelectedIndex = 2;
      FlowControlSelectionBox.SelectedIndex = 2;
    }

    /// <summary>
    /// Обновляет VID и PID при выборе COM-порта.
    /// </summary>
    private void COMPortSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (COMPortSelectionBox.SelectedItem is string selectedPort)
      {
        LoadVidPid(selectedPort);
      }
    }

    /// <summary>
    /// Считывает VID и PID устройства, которому принадлежит указанный COM-порт.
    /// </summary>
    /// <param name="comPort">Имя COM-порта.</param>
    private void LoadVidPid(string comPort)
    {
      VIDData.Text = "N/A";
      PIDData.Text = "N/A";
      string query = $"SELECT DeviceID FROM Win32_PnPEntity WHERE Name LIKE '%({comPort})%'";

      using var searcher = new ManagementObjectSearcher(query);
      foreach (ManagementObject device in searcher.Get())
      {
        string deviceId = device["DeviceID"]?.ToString() ?? string.Empty;
        var match = System.Text.RegularExpressions.Regex.Match(
          deviceId,
          @"VID_([0-9A-F]{4})&PID_([0-9A-F]{4})",
          System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (!match.Success)
        {
          continue;
        }

        VIDData.Text = match.Groups[1].Value;
        PIDData.Text = match.Groups[2].Value;
        return;
      }
    }

    /// <summary>
    /// Выбирает значение списка по текстовому представлению свойства модели.
    /// </summary>
    private static void SetValueFromProperty(Type modelType, object deviceModel, string propertyName, ComboBox comboBox)
    {
      object? value = modelType.GetProperty(propertyName)?.GetValue(deviceModel);
      if (value != null)
      {
        SetValue(comboBox, value.ToString() ?? string.Empty);
      }
    }

    /// <summary>
    /// Выбирает элемент списка с указанным текстом.
    /// </summary>
    private static void SetValue(ComboBox comboBox, string text)
    {
      foreach (object item in comboBox.Items)
      {
        string itemText = item is ComboBoxItem comboBoxItem
          ? comboBoxItem.Content?.ToString() ?? string.Empty
          : item?.ToString() ?? string.Empty;
        if (string.Equals(itemText, text, StringComparison.OrdinalIgnoreCase))
        {
          comboBox.SelectedItem = item;
          return;
        }
      }
    }

    /// <summary>
    /// Возвращает текст выбранного элемента списка.
    /// </summary>
    private static string GetSelectedText(ComboBox comboBox) =>
      comboBox.SelectedItem is ComboBoxItem item ? item.Content?.ToString() ?? string.Empty : string.Empty;

    /// <summary>
    /// Возвращает выбранное целочисленное значение или -1 при ошибке преобразования.
    /// </summary>
    private static int GetIntegerValue(ComboBox comboBox) =>
      int.TryParse(GetSelectedText(comboBox), out int value) ? value : -1;
  }
}
