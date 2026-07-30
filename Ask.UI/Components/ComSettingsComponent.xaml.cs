using Ask.Core.Shared.DTO.Devices.Base;
using Ask.Device.Communication.Com.Configuration;
using System.IO.Ports;
using System.Management;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
      AddHandler(
        ComboBox.SelectionChangedEvent,
        new SelectionChangedEventHandler(OnSettingsSelectionChanged));
      Reset();
    }

    /// <summary>
    /// Возникает при изменении одного из параметров COM-порта.
    /// </summary>
    public event EventHandler? SettingsChanged;

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
    /// Возвращает выбранный режим управления потоком данных.
    /// </summary>
    public Handshake Handshake => GetSelectedText(FlowControlSelectionBox) switch
    {
      "Xon/Xoff" => Handshake.XOnXOff,
      "Аппаратное" => Handshake.RequestToSend,
      _ => Handshake.None,
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
      if (deviceModel is not IComPortSettingsProvider provider)
      {
        throw new InvalidOperationException($"COM-устройство {deviceModel.GetType().Name} не задаёт настройки порта по умолчанию.");
      }

      Load(provider.DefaultComPortSettings);
    }

    /// <summary>
    /// Загружает параметры последовательного порта,
    /// оставляя выбор физического COM-порта пользователю.
    /// </summary>
    /// <param name="settings">Параметры последовательного порта.</param>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается, если <paramref name="settings"/> равен <see langword="null"/>.
    /// </exception>
    public void Load(ComPortSettings settings)
    {
      ArgumentNullException.ThrowIfNull(settings);
      SetValue(BaudRateSelectionBox, settings.BaudRate.ToString());
      SetValue(DataBitsSelectionBox, settings.DataBits.ToString());
      SetValue(StopBitsSelectionBox, ParseStopBits(settings.StopBits) switch
      {
        StopBits.OnePointFive => "1.5",
        StopBits.Two => "2",
        _ => "1",
      });
      SetValue(ParitySelectionBox, ParseParity(settings.Parity) switch
      {
        Parity.Even => "Чет",
        Parity.Odd => "Нечет",
        Parity.Mark => "Маркер",
        Parity.Space => "Пробел",
        _ => "Нет",
      });
      SetFlowControl(ParseHandshake(settings.Handshake));
    }

    /// <summary>
    /// Загружает сохранённые параметры последовательного порта в компонент.
    /// </summary>
    /// <param name="settings">Сохранённые параметры COM-порта.</param>
    public void Load(SerialPortCustom settings, bool loadAvailablePorts = true)
    {
      ArgumentNullException.ThrowIfNull(settings);
      if (loadAvailablePorts)
      {
        LoadAvailablePorts();
      }

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
      SetFlowControl(settings.Handshake);
    }

    private void SetFlowControl(Handshake handshake)
    {
      SetValue(FlowControlSelectionBox, handshake switch
      {
        Handshake.XOnXOff => "Xon/Xoff",
        Handshake.RequestToSend or Handshake.RequestToSendXOnXOff => "Аппаратное",
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

      return new SerialPortCustom(PortName, BaudRate, Parity, DataBits, StopBits)
      {
        Handshake = Handshake,
      };
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
    /// Показывает или удаляет подсветку ошибки для секции параметров COM-порта.
    /// </summary>
    /// <param name="isInvalid">Признак наличия ошибки в параметрах COM-порта.</param>
    public void SetValidationHighlight(bool isInvalid)
    {
      if (isInvalid)
      {
        COMContainer.BorderBrush = TryFindResource("RedColorSolidColorBrush") as Brush ?? Brushes.IndianRed;
        COMContainer.BorderThickness = new Thickness(2);
        return;
      }

      COMContainer.ClearValue(Border.BorderBrushProperty);
      COMContainer.ClearValue(Border.BorderThicknessProperty);
    }

    /// <summary>
    /// Оповещает подписчиков об изменении параметров COM-порта.
    /// </summary>
    /// <param name="sender">Источник события.</param>
    /// <param name="e">Аргументы изменения выбранного значения.</param>
    private void OnSettingsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      SettingsChanged?.Invoke(this, EventArgs.Empty);
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

    /// <summary>
    /// Преобразует строковое представление режима контроля чётности
    /// в значение перечисления <see cref="Parity"/>.
    /// </summary>
    /// <param name="value">Строковое представление режима контроля чётности.</param>
    /// <returns>
    /// Соответствующее значение <see cref="Parity"/>.
    /// Если преобразование невозможно, возвращается <see cref="Parity.None"/>.
    /// </returns>
    private static Parity ParseParity(string value) =>
      Enum.TryParse(value, true, out Parity result) ? result : Parity.None;

    /// <summary>
    /// Преобразует строковое представление количества стоп-битов
    /// в значение перечисления <see cref="StopBits"/>.
    /// </summary>
    /// <param name="value">Строковое представление количества стоп-битов.</param>
    /// <returns>
    /// Соответствующее значение <see cref="StopBits"/>.
    /// Если преобразование невозможно, возвращается <see cref="StopBits.One"/>.
    /// </returns>
    private static StopBits ParseStopBits(string value) =>
      Enum.TryParse(value, true, out StopBits result) ? result : StopBits.One;

    /// <summary>
    /// Преобразует строковое представление режима управления потоком данных
    /// в значение перечисления <see cref="Handshake"/>.
    /// </summary>
    /// <param name="value">Строковое представление режима управления потоком данных.</param>
    /// <returns>
    /// Соответствующее значение <see cref="Handshake"/>.
    /// Если преобразование невозможно, возвращается <see cref="Handshake.None"/>.
    /// </returns>
    private static Handshake ParseHandshake(string value) =>
      Enum.TryParse(value, true, out Handshake result) ? result : Handshake.None;
  }
}
